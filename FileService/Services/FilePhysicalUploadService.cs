using System.Text.Json;
using FileService.Configuration;
using FileService.Services.Data;
using FileService.Utils;
using Infrastructure.Databases;
using Infrastructure.Response;
using Infrastructure.Services;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using StackExchange.Redis;
using FsFile = System.IO.File;
using DbFile = Infrastructure.Databases.File;

namespace FileService.Services;

public record PyhsicalUploadMetaData(long FileSize, string FileName, string DirId, string FileHash);
public record FileInfo(string FileName, long FileSize, string FileHash, string DirId);

public class FilePhysicalUploadService(
    FileDbService fileDbService,
    DirectoryItemDbService directoryItemDbService,
    DirectoryItemOperationCheckService directoryItemOperationCheckService,
    RedisContext redisContext,
    IHttpContextAccessor httpContextAccessor,
    IMinioClient minioClient,
    AppConfig appConfig,
    IServiceProvider serviceProvider,
    MsSqlContext msSqlContext,
    HttpContextService httpContextService)
{
    private readonly RedisContext _redisContext = redisContext;
    private readonly FileDbService _fileDbService = fileDbService;
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;
    private readonly DirectoryItemOperationCheckService _directoryItemOperationCheckService = directoryItemOperationCheckService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IMinioClient _minioClient = minioClient;
    private readonly AppConfig _appConfig = appConfig;
    private readonly MsSqlContext _msSqlContext = msSqlContext;
    private readonly HttpContextService _httpContextService = httpContextService;

    private const long ChunkSize = 16 * 1024 * 1024; // 默认文件分片大小为 16 MB
    private static readonly TimeSpan ExpireTimeSpan = TimeSpan.FromHours(48);
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private const int HashBlockSize = 32;
    private const int HeadHashEndPosition = HashBlockSize * 32; // 头部哈希计算范围为32个哈希块

    private static string ChunkInfoKey(string tempFileNameHash) => $"chunk:{tempFileNameHash}";
    private static string FileInfoKey(string tempFileNameHash) => $"fileInfo:{tempFileNameHash}";
    private static string TempFileKey(string tempFileNameHash) => $"tempFile:{tempFileNameHash}";

    public async Task<dynamic> GetChunkDescriptionAsync(PyhsicalUploadMetaData metaData)
    {
        var fileSize = metaData.FileSize;
        var fileName = metaData.FileName;
        var dirId = metaData.DirId;

        var checker = (await _directoryItemOperationCheckService.CreateItemCheckerAsync(dirId, true))!;
        if (!checker.HasPermission().State)
        {
            return AppResponse.PermissionDenied();
        }

        if (!(await checker.IsUniqueNameAsync(fileName)).State)
        {
            return AppResponse.DuplicateDirectoryItem("文件名已存在");
        }

        var uid = _httpContextService.Uid();
        var user = _msSqlContext.Users.Single(u => u.Id == uid);
        if ((user.TotalSpace - user.UsedSpace) < fileSize)
        {
            return AppResponse.SpaceNotEnough();
        }

        var chunkCount = CalcChunkCount(fileSize);
        var tempFileName = await FileHelper.CreatePreFillFile(fileSize);
        var hashEntries = new HashEntry[chunkCount];
        for (var i = 0; i < chunkCount; i++)
        {
            hashEntries[i] = new HashEntry(i, string.Empty);
        }

        var tempFileNameHash = await HashValue.Sha256Async(tempFileName);

        var chunkInfoKey = ChunkInfoKey(tempFileNameHash);
        var cacheChunkInfoTask = await _redisContext.FileUploadMetaData.HashSetAsync(chunkInfoKey, hashEntries)
            .ContinueWith(_ => _redisContext.FileUploadMetaData.KeyExpireAsync(chunkInfoKey, ExpireTimeSpan));

        var fileInfoKey = FileInfoKey(tempFileNameHash);
        var cacheFileInfoTask = _redisContext.FileUploadMetaData.StringSetAsync(
            fileInfoKey,
            JsonSerializer.Serialize(new FileInfo(fileName, fileSize, metaData.FileHash, dirId)),
            ExpireTimeSpan);

        var tempFileKey = TempFileKey(tempFileNameHash);
        var cacheTempFileNameTask =
            _redisContext.FileUploadMetaData.StringSetAsync(tempFileKey, tempFileName, ExpireTimeSpan);

        Task.WaitAll([cacheChunkInfoTask, cacheFileInfoTask, cacheTempFileNameTask]);

        return new
        {
            ChunkSize = ChunkSize,
            ChunkCount = chunkCount,
            UploadKey = tempFileNameHash
        };
    }

    private static long CalcChunkCount(long fileSize) =>
        fileSize % ChunkSize == 0 ? fileSize / ChunkSize : (fileSize / ChunkSize) + 1;

    private async Task<string?> TempFileName(string uploadKey) =>
        await _redisContext.FileUploadMetaData.StringGetAsync(TempFileKey(uploadKey));

    private static async Task WriteChunkToTempFile(int serial, string tempFileName, Stream chunkStream)
    {
        await using var fs = FsFile.OpenWrite(tempFileName);
        fs.Seek(serial * ChunkSize, SeekOrigin.Begin);
        chunkStream.Seek(0, SeekOrigin.Begin);
        await chunkStream.CopyToAsync(fs);
    }

    public async Task<AppResponse> UploadChunkAsync(string uploadKey, int serial)
    {
        var body = _httpContextAccessor.HttpContext!.Request.Body;
        using var memStream = new MemoryStream();
        await body.CopyToAsync(memStream);

        var tempFile = await TempFileName(uploadKey);
        if (tempFile is null)
        {
            return AppResponse.InvalidArgument();
        }

        memStream.Seek(0, SeekOrigin.Begin);
        var chunkHash = await HashValue.Sha256Async(memStream);
        _ = _redisContext.FileUploadMetaData.HashSetAsync(ChunkInfoKey(uploadKey), serial, chunkHash);
        await WriteChunkToTempFile(serial, tempFile, memStream);
        return AppResponse.Success(new { Hash = chunkHash });
    }

    public async Task<AppResponse> UploadConfirmAsync(string uploadKey)
    {
        var tempFile = await TempFileName(uploadKey);
        if (tempFile is null || !FsFile.Exists(tempFile))
        {
            return AppResponse.InvalidArgument();
        }

        string? cachedFileInfo = await _redisContext.FileUploadMetaData.StringGetAsync(FileInfoKey(uploadKey));
        if (cachedFileInfo is null)
        {
            return AppResponse.InvalidArgument();
        }

        var fileInfo = JsonSerializer.Deserialize<FileInfo>(cachedFileInfo);
        if (fileInfo is null)
        {
            return AppResponse.InvalidArgument();
        }

        await using var fs = FsFile.OpenRead(tempFile);
        var fileHash = await HashValue.Sha256Async(fs);
        if (!fileInfo.FileHash.Equals(fileHash, StringComparison.CurrentCultureIgnoreCase))
        {
            var chunkInfos = await _redisContext.FileUploadMetaData.HashGetAllAsync(ChunkInfoKey(uploadKey));
            var chunkDict = chunkInfos.ToDictionary(chunkInfo =>
                Convert.ToInt32(chunkInfo.Name), chunkInfo => ((string?)chunkInfo.Value) ?? string.Empty);
            return AppResponse.IncompleteFile(chunkDict);
        }

        var headHash = await CalcHeadHashAsync(tempFile);
        var file = await CreateNewFileAsync(headHash, fileInfo);
        _ = SaveToOssAsync(tempFile, file.Id.ToString());

        var uid = _httpContextService.Uid();
        await _directoryItemDbService.CreateDirectoryFileAsync(
            uid,
            fileInfo.DirId,
            file.Id.ToString(),
            fileInfo.FileName
        );
        var user = _msSqlContext.Users.Single(u => u.Id == uid);
        user.UsedSpace += fileInfo.FileSize;
        await _msSqlContext.SaveChangesAsync();

        await _redisContext.FileUploadMetaData.KeyDeleteAsync([
            ChunkInfoKey(uploadKey),
            FileInfoKey(uploadKey),
            TempFileKey(uploadKey)
        ]);
        return AppResponse.Success();
    }

    private static async Task<string> CalcHeadHashAsync(string tempFileName)
    {
        await using var fs = FsFile.OpenRead(tempFileName);
        var buffer = new byte[HeadHashEndPosition];
        var sz = await fs.ReadAsync(buffer);
        return await HashValue.Sha256Async(new MemoryStream(buffer[.. sz]));
    }

    private async Task<DbFile> CreateNewFileAsync(string headHash, FileInfo fileInfo)
    {
        var file = new DbFile
        {
            HeadHash = headHash,
            EntiretyHash = fileInfo.FileHash,
            FileSize = fileInfo.FileSize,
            ReferenceCount = 1,
            UploadingLock = true,
            Enable = true
        };
        try
        {
            await _fileDbService.CreateFileAsync(file);
        }
        catch (DbUpdateException)
        {
            file = (await _fileDbService.GetFileAsync(headHash, fileInfo.FileHash))!;
            await _fileDbService.IncreaseReferenceCountAsync(file.Id.ToString());
        }
        return file;
    }

    private async Task SaveToOssAsync(string tempFileName, string fileId)
    {
        using var scoped = _serviceProvider.CreateScope();
        var fileDbService = scoped.ServiceProvider.GetRequiredService<FileDbService>();
        await using var fs = FsFile.OpenRead(tempFileName);
        await _minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_appConfig.MinIO.DefaultBucket)
                .WithStreamData(fs)
                .WithObjectSize(fs.Length)
                .WithObject(fileId)
        );
        await fileDbService.SetFileUploadingLock(fileId, false);
        fs.Close();
        FsFile.Delete(tempFileName);
    }
}