using FileService.Controllers;
using FileService.Services.Data;
using Infrastructure.Databases;
using Infrastructure.Response;
using Infrastructure.Services;

namespace FileService.Services;

public record DirectoryItemData(string Id, string Name, bool IsFile, string? FileId);

public class DirectoryItemService(
    DirectoryItemDbService directoryItemDbService,
    HttpContextService httpContextService,
    DirectoryItemOperationCheckService directoryItemOperationCheckService,
    RecycleBinDbService recycleBinDbService)
{
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;
    private readonly HttpContextService _httpContextService = httpContextService;

    private readonly DirectoryItemOperationCheckService _directoryItemOperationCheckService =
        directoryItemOperationCheckService;

    private readonly RecycleBinDbService _recycleBinDbService = recycleBinDbService;

    public async Task<AppResponse> GetRootIdAsync()
    {
        var uid = _httpContextService.Uid();
        var root = await _directoryItemDbService.GetRootAsync(uid);
        return AppResponse.Success(new
        {
            RootId = root.Id.ToString()
        });
    }

    public async Task<AppResponse> BrowseDirectoryAsync(string itemId)
    {
        var checker = (await _directoryItemOperationCheckService.CreateItemCheckerAsync(itemId, true))!;
        var hasPermission = checker.HasPermission().State;
        if (!hasPermission)
        {
            return AppResponse.PermissionDenied();
        }

        var dbItems = await _directoryItemDbService.GetChildrenAsync(itemId);
        var resultItems = dbItems
            .Select(i => new DirectoryItemData(i.Id.ToString(), i.Name, i.IsFile, i.FileId.ToString()))
            .ToList();
        return AppResponse.Success(resultItems);
    }

    public async Task<AppResponse> CreateDirectoryAsync(CreateDirectoryArgs createDirectoryArgs)
    {
        var dirName = createDirectoryArgs.Name;
        var parentId = createDirectoryArgs.ParentId;

        var appResponse = await _directoryItemOperationCheckService.CommonCheckAsync(parentId, dirName);
        if (appResponse is not null)
        {
            return appResponse;
        }

        await _directoryItemDbService.CreateDirectoryAsync(_httpContextService.Uid(), parentId, dirName);
        return AppResponse.Success();
    }

    public async Task<AppResponse> RenameDirectoryItemAsync(RenameItemArgs renameItemArgs, string itemId)
    {
        var newName = renameItemArgs.NewName;
        var item = await _directoryItemDbService.GetAsync(itemId);
        var parentId = item?.ParentId;
        if (parentId is null)
        {
            return AppResponse.InvalidArgument();
        }

        var appResponse = await _directoryItemOperationCheckService.CommonCheckAsync(parentId.ToString(), newName);
        if (appResponse is not null)
        {
            return appResponse;
        }

        await _directoryItemDbService.RenameAsync(itemId, newName);
        return AppResponse.Success();
    }

    private async Task<bool> IsDescendantAsync(string ancestorId, string descendantId)
    {
        var ancestorPath = await _directoryItemDbService.GetPathAsync(ancestorId);
        var descendantPath = await _directoryItemDbService.GetPathAsync(descendantId);
        return descendantPath != ancestorPath && descendantPath.StartsWith(ancestorPath);
    }

    public async Task<AppResponse> MoveDirectoryItemAsync(string itemId, MoveItemArgs moveItemArgs)
    {
        var targetDirId = moveItemArgs.TargetDirId;
        var checker = (await _directoryItemOperationCheckService.CreateItemCheckerAsync(itemId, true))!;
        if (!checker.HasPermission().State)
        {
            return AppResponse.PermissionDenied();
        }

        if (await IsDescendantAsync(itemId, targetDirId))
        {
            return AppResponse.InvalidArgument();
        }

        var item = checker.DirectoryItem();
        var appResponse = await _directoryItemOperationCheckService.CommonCheckAsync(targetDirId, item.Name);
        if (appResponse is not null)
        {
            return appResponse;
        }

        var itemPath = await _directoryItemDbService.GetPathAsync(itemId);
        var targetDirPath = await _directoryItemDbService.GetPathAsync(targetDirId);

        if (targetDirPath.StartsWith(itemPath))
        {
            return AppResponse.InvalidPath();
        }

        await _directoryItemDbService.MoveAsync(itemId, targetDirId);
        return AppResponse.Success();
    }

    public async Task<AppResponse> MoveToRecycleBinAsync(string itemId)
    {
        var appResponse = await _directoryItemOperationCheckService.CheckPermissionAsync(itemId);
        if (appResponse is not null)
        {
            return appResponse;
        }

        await _recycleBinDbService.MoveToRecycleBinAsync(itemId);
        return AppResponse.Success();
    }

    public async Task<AppResponse> SearchAsync(string searchStr)
    {
        var uid = _httpContextService.Uid();
        var searchResults = await _directoryItemDbService.SearchAsync(uid, searchStr);
        var results = new List<dynamic>();
        foreach (var item in searchResults)
        {
            var id = item.Id.ToString();
            var result = new
            {
                Id = id,
                item.Name,
                DirectoryPath = await _directoryItemDbService.GetDirectorytPathAsync(id),
                item.IsFile,
                item.FileId
            };
            results.Add(result);
        }

        return AppResponse.Success(results);
    }

    public async Task<AppResponse> GetPathItemsAsync(string itemId)
    {
        var appResponse = await _directoryItemOperationCheckService.CheckPermissionAsync(itemId);
        if (appResponse is not null)
        {
            return appResponse;
        }

        var dirItems = new List<DirectoryItem>();
        var item = (await _directoryItemDbService.GetAsync(itemId))!;
        while (item.ParentId is not null)
        {
            dirItems.Add(item);
            item = (await _directoryItemDbService.GetAsync(item.ParentId.ToString()!))!;
        }

        dirItems.Reverse();
        var pathItems = new List<dynamic>();
        for (var i = 0; i < dirItems.Count; i++)
        {
            pathItems.Add(new
            {
                Order = i,
                Id = dirItems[i].Id,
                Name = dirItems[i].Name,
                IsFile = dirItems[i].IsFile,
                FileId = dirItems[i].FileId
            });
        }

        return AppResponse.Success(pathItems);
    }
}