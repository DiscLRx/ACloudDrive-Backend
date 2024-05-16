using FileService.Configuration;
using FileService.Services.Data;
using Infrastructure.Response;
using Infrastructure.Services;
using Minio;
using Minio.DataModel.Args;

namespace FileService.Services;

public class FileDownloadService(
    HttpContextService httpContextService,
    FileDbService fileDbService,
    DirectoryItemDbService directoryItemDbService,
    IMinioClient minioClient,
    AppConfig appConfig)
{
    private readonly AppConfig _appConfig = appConfig;
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;
    private readonly FileDbService _fileDbService = fileDbService;
    private readonly HttpContextService _httpContextService = httpContextService;
    private readonly IMinioClient _minioClient = minioClient;

    public async Task<AppResponse> GetPresignedFileUrlByDirItemIdAsync(string itemId)
    {
        var uid = _httpContextService.Uid();
        var item = await _directoryItemDbService.GetAsync(itemId);
        if (item is null || item.Uid != uid)
        {
            return AppResponse.PermissionDenied();
        }

        if (!item.IsFile)
        {
            return AppResponse.InvalidArgument();
        }

        var file = (await _fileDbService.GetFileAsync(item.FileId.ToString()!))!;
        if (file.UploadingLock)
        {
            return AppResponse.WaitingSaveToOss();
        }

        if (!file.Enable)
        {
            return AppResponse.FileDisabled();
        }

        var getObjArgs = new PresignedGetObjectArgs()
            .WithBucket(_appConfig.MinIO.DefaultBucket)
            .WithObject(file.Id.ToString())
            .WithExpiry(604800);
        var presignedUrl = await _minioClient.PresignedGetObjectAsync(getObjArgs);
        return AppResponse.Success(new
        {
            Url = presignedUrl
        });
    }

    public async Task<AppResponse> GetPresignedFileUrlByFileIdAsync(string id)
    {
        var file = await _fileDbService.GetFileAsync(id);
        if (file is null)
        {
            return AppResponse.InvalidArgument();
        }
        var getObjArgs = new PresignedGetObjectArgs()
            .WithBucket(_appConfig.MinIO.DefaultBucket)
            .WithObject(file.Id.ToString())
            .WithExpiry(604800);
        var presignedUrl = await _minioClient.PresignedGetObjectAsync(getObjArgs);
        return AppResponse.Success(new
        {
            Url = presignedUrl
        });
    }
}