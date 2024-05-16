using FileService.Services.Data;
using FileService.Utils;
using Infrastructure.Databases;
using Infrastructure.Response;
using Infrastructure.Services;

namespace FileService.Services;

public class DirectoryItemOperationCheckService(DirectoryItemDbService directoryItemDbService, HttpContextService httpContextService)
{
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;
    private readonly HttpContextService _httpContextService = httpContextService;

    public async Task<DirectoryItemChecker?> CreateItemCheckerAsync(string itemId, bool ignoreNull)
    {
        var directoryItem = await _directoryItemDbService.GetAsync(itemId);
        if (directoryItem is not null)
        {
            return new DirectoryItemChecker(
                directoryItem,
                _directoryItemDbService,
                _httpContextService);
        }
        if (ignoreNull)
        {
            return new DirectoryItemChecker(
                new DirectoryItem(),
                _directoryItemDbService,
                _httpContextService,
                false);
        }
        return null;
    }

    public async Task<AppResponse?> CommonCheckAsync(string itemId, string childItemName)
    {
        var checker = (await CreateItemCheckerAsync(itemId, true))!;
        if (!checker.HasPermission().State)
        {
            return AppResponse.PermissionDenied();
        }

        if (DirectoryItemNameCheck.IsInvalidName(childItemName))
        {
            return AppResponse.InvalidArgument();
        }

        if (!(await checker.IsUniqueNameAsync(childItemName)).State)
        {
            return AppResponse.DuplicateDirectoryItem("目录项名称已存在");
        }

        return null;
    }

    public async Task<AppResponse?> CheckPermissionAsync(string itemId)
    {
        var checker = (await CreateItemCheckerAsync(itemId, true))!;
        if (!checker.HasPermission().State)
        {
            return AppResponse.PermissionDenied();
        }
        return null;
    }
}

public class DirectoryItemChecker(
    DirectoryItem directoryItem,
    DirectoryItemDbService directoryItemDbService,
    HttpContextService httpContextService,
    bool state = true)
{
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;
    private readonly HttpContextService _httpContextService = httpContextService;
    private readonly DirectoryItem _directoryItem = directoryItem;
    private bool _state = state;
    public bool State => _state;

    public DirectoryItem DirectoryItem() => _directoryItem;

    public DirectoryItemChecker HasPermission()
    {
        _state &= _directoryItem.Uid == _httpContextService.Uid();
        return this;
    }

    public DirectoryItemChecker IsFile()
    {
        _state &= _directoryItem.IsFile;
        return this;
    }

    public DirectoryItemChecker IsDirectory()
    {
        _state &= !_directoryItem.IsFile;
        return this;
    }

    public async Task<DirectoryItemChecker> IsUniqueNameAsync(string name)
    {
        _state &= !(await _directoryItemDbService.NameExistsAsync(_directoryItem.Id.ToString(), name));
        return this;
    }
}