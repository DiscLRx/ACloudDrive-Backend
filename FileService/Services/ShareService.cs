using FileService.Controllers;
using FileService.Services.Data;
using Infrastructure.Databases;
using Infrastructure.Response;
using Infrastructure.Services;

namespace FileService.Services;

public class ShareService(
    DirectoryItemDbService directoryItemDbService,
    HttpContextService httpContextService,
    ShareDbService shareDbService,
    DirectoryItemOperationCheckService directoryItemOperationCheckService)
{
    private readonly ShareDbService _shareDbService = shareDbService;
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;
    private readonly HttpContextService _httpContextService = httpContextService;

    private readonly DirectoryItemOperationCheckService _directoryItemOperationCheckService =
        directoryItemOperationCheckService;

    public async Task<AppResponse> CreateShareAsync(CreateShareArgs createShareArgs)
    {
        var itemId = createShareArgs.ItemId;
        var appResponse = await _directoryItemOperationCheckService.CheckPermissionAsync(itemId);
        if (appResponse is not null)
        {
            return appResponse;
        }

        var expireDate = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(createShareArgs.Ts), TimeZoneInfo.Local).DateTime;
        var share = new Share
        {
            DirItemId = new Guid(itemId),
            Key = createShareArgs.Key,
            ExpireDate = expireDate
        };
        await _shareDbService.CreateAsync(share);

        return AppResponse.Success(new
        {
            Code = share.Id
        });
    }


    public async Task<AppResponse> SaveShareAsync(SaveShareArgs saveShareArgs)
    {
        var share = await _shareDbService.GetAsync(saveShareArgs.Code);
        if (share is null || share.Key != saveShareArgs.Key || share.ExpireDate <= DateTime.Now)
        {
            return AppResponse.PermissionDenied();
        }

        var dirId = saveShareArgs.DirId;
        var item = (await _directoryItemDbService.GetAsync(share.DirItemId.ToString()))!;
        var appResponse = await _directoryItemOperationCheckService.CommonCheckAsync(dirId, item.Name);
        if (appResponse is not null)
        {
            return appResponse;
        }

        await _directoryItemDbService.CopyAsync(share.DirItemId.ToString(), saveShareArgs.DirId);
        return AppResponse.Success();
    }

    public async Task<AppResponse> GetShareInformationAsync(string code, string key)
    {
        var share = await _shareDbService.GetAsync(code);
        if (share is null || share.Key != key || share.ExpireDate <= DateTime.Now)
        {
            return AppResponse.PermissionDenied();
        }

        var item = (await _directoryItemDbService.GetAsync(share.DirItemId.ToString()))!;
        var size = await _directoryItemDbService.CalculateSizeAsync(share.DirItemId.ToString());
        return AppResponse.Success(new
        {
            Name = item.Name,
            IsFile = item.IsFile,
            Size = size,
            Expire = new DateTimeOffset(share.ExpireDate).ToUnixTimeMilliseconds()
        });
    }

    public async Task<AppResponse> GetShareDetailAsync(string code)
    {
        var share = await _shareDbService.GetAsync(code);
        if (share is null || share.ExpireDate <= DateTime.Now)
        {
            return AppResponse.PermissionDenied();
        }
        var item = (await _directoryItemDbService.GetAsync(share.DirItemId.ToString()))!;
        var uid = _httpContextService.Uid();
        if (item.Uid != uid)
        {
            return AppResponse.PermissionDenied();
        }

        var itemId = item.Id.ToString();
        var result = new
        {
            Code = share.Id,
            Key = share.Key,
            Expire = new DateTimeOffset(share.ExpireDate).ToUnixTimeMilliseconds(),
            Name = item.Name,
            DirectoryPath = await _directoryItemDbService.GetDirectorytPathAsync(itemId),
            Size = await _directoryItemDbService.CalculateSizeAsync(itemId),
            IsFile = item.IsFile
        };
        return AppResponse.Success(result);
    }

    public async Task<AppResponse> GetShareListAsync()
    {
        var uid = _httpContextService.Uid();
        var shares = await _shareDbService.GetListAsync(uid);
        var results = new List<dynamic>();
        foreach (var share in shares)
        {
            results.Add(new
            {
                Code = share.Code,
                Expire = share.Expire,
                Name = share.Name,
                DirectoryPath = await _directoryItemDbService.GetDirectorytPathAsync(share.DirectoryItemId),
                IsFile = share.IsFile
            });
        }
        return AppResponse.Success(results);
    }

    public async Task<AppResponse> DeleteShareAsync(string code)
    {
        var uid = _httpContextService.Uid();
        var share = await _shareDbService.GetAsync(code);
        if (share is null)
        {
            return AppResponse.InvalidArgument();
        }

        var item = await _directoryItemDbService.GetAsync(share.DirItemId.ToString());
        if (item!.Uid != uid)
        {
            return AppResponse.PermissionDenied();
        }

        await _shareDbService.DeleteAsync(code);
        return AppResponse.Success();
    }

}