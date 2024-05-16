using FileService.Services.Data;
using Infrastructure.Response;
using Infrastructure.Services;

namespace FileService.Services;

public class RecycleBinService(
    RecycleBinDbService recycleBinDbService,
    HttpContextService httpContextService)
{
    private readonly RecycleBinDbService _recycleBinDbService = recycleBinDbService;
    private readonly HttpContextService _httpContextService = httpContextService;

    public async Task<AppResponse> BrowseRecycleBinAsync()
    {
        var uid = _httpContextService.Uid();
        var recycleItems = await _recycleBinDbService.BrowseAsync(uid);
        var results = new List<dynamic>();
        foreach (var item in recycleItems)
        {
            results.Add(new
            {
                Id = item.Id,
                Name = item.Name,
                IsFile = item.IsFile,
                DeleteTs = new DateTimeOffset(item.DeleteDate).ToUnixTimeMilliseconds(),
                DirectoryPath = item.DirPath
            });
        }
        return AppResponse.Success(results);
    }


    public async Task<AppResponse> RestoreFromRecycleBinAsync(string recycleBinId)
    {
        var uid = _httpContextService.Uid();
        var recycleBinItem = await _recycleBinDbService.GetAsync(recycleBinId);
        if (recycleBinItem?.Uid != uid || !recycleBinItem.IsRecycleRoot)
        {
            return AppResponse.PermissionDenied();
        }

        var success = await _recycleBinDbService.RestoreAsync(recycleBinId);
        return success ? AppResponse.Success() : AppResponse.DuplicateDirectoryItem();
    }


    public async Task<AppResponse> DeleteForeverAsync(string recycleBinId)
    {
        var uid = _httpContextService.Uid();
        var recycleBinItem = await _recycleBinDbService.GetAsync(recycleBinId);
        if (recycleBinItem?.Uid != uid || !recycleBinItem.IsRecycleRoot)
        {
            return AppResponse.PermissionDenied();
        }

        await _recycleBinDbService.DeleteForeverAsync(recycleBinId);
        return AppResponse.Success();
    }
}