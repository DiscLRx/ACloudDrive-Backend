using FileService.Services.Data;
using Infrastructure.Response;

namespace FileService.Services;

public class FileManageService(FileDbService fileDbService)
{
    private readonly FileDbService _fileDbService = fileDbService;

    public async Task<AppResponse> GetFilesAsync()
    {
        return AppResponse.Success(await _fileDbService.GetFilesAsync());
    }

    public async Task<AppResponse> SetFileEnableAsync(string id, bool enable)
    {
        await _fileDbService.SetFileEnableAsync(id, enable);
        return AppResponse.Success();
    }
}