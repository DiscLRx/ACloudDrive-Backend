using FileService.Controllers;
using FileService.Services.Data;
using Infrastructure.Databases;
using Infrastructure.Response;
using Infrastructure.Services;

namespace FileService.Services;

public class FileHashUploadService(
    FileDbService fileDbService,
    DirectoryItemDbService directoryItemDbService,
    DirectoryItemOperationCheckService directoryItemOperationCheckService,
    MsSqlContext msSqlContext,
    FilePhysicalUploadService physicalUploadService,
    HttpContextService httpContextService)
{
    private readonly FileDbService _fileDbService = fileDbService;
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;
    private readonly DirectoryItemOperationCheckService _directoryItemOperationCheckService = directoryItemOperationCheckService;
    private readonly MsSqlContext _msSqlContext = msSqlContext;
    private readonly FilePhysicalUploadService _physicalUploadService = physicalUploadService;
    private readonly HttpContextService _httpContextService = httpContextService;

    public async Task<AppResponse> HashUploadAsync(HashUploadArgs hashUploadArgs)
    {
        var fileName = hashUploadArgs.FileName;
        var dirId = hashUploadArgs.DirId;

        var appResponse = await _directoryItemOperationCheckService.CommonCheckAsync(dirId, fileName);
        if (appResponse is not null)
        {
            return appResponse;
        }

        var file = await _fileDbService.GetFileAsync(hashUploadArgs.HeadHash, hashUploadArgs.EntiretyHash);
        var uid = _httpContextService.Uid();
        var user = _msSqlContext.Users.Single(u => u.Id == uid);
        if ((user.TotalSpace - user.UsedSpace) < (file?.FileSize ?? hashUploadArgs.FileSize))
        {
            return AppResponse.SpaceNotEnough();
        }

        if (file is null)
        {
            var chunkDescription =
                await _physicalUploadService.GetChunkDescriptionAsync(
                    new PyhsicalUploadMetaData(
                        hashUploadArgs.FileSize,
                        hashUploadArgs.FileName,
                        hashUploadArgs.DirId,
                        hashUploadArgs.HeadHash,
                        hashUploadArgs.EntiretyHash
                    )
                );
            return AppResponse.NeedPyhsicalUpload(chunkDescription);
        }

        await _directoryItemDbService.CreateDirectoryFileAsync(uid, dirId, file.Id.ToString(), fileName);
        user.UsedSpace += file.FileSize;
        await _fileDbService.IncreaseReferenceCountAsync(file.Id.ToString());
        await _msSqlContext.SaveChangesAsync();

        return AppResponse.Success();
    }
}