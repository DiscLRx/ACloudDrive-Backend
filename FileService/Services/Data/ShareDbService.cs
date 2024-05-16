using Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace FileService.Services.Data;

public class ShareDbService(MsSqlContext msSqlContext, DirectoryItemDbService directoryItemDbService)
{
    private readonly MsSqlContext _msSqlContext = msSqlContext;
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;

    private static Guid ToGuid(string id) => new(id);

    public async Task CreateAsync(Share share)
    {
        await _msSqlContext.Shares.AddAsync(share);
        await _msSqlContext.SaveChangesAsync();
    }

    public async Task<Share?> GetAsync(string id)
    {
        var guid = ToGuid(id);
        return await _msSqlContext.Shares.SingleOrDefaultAsync(s => s.Id == guid);
    }

    public async Task<dynamic> GetListAsync(long uid)
    {
        var now = DateTime.Now;
        Console.WriteLine(now);
        return await _msSqlContext.Shares
            .Join(_msSqlContext.DirectoryItems,
                s => s.DirItemId,
                i => i.Id,
                (s, i) => new { Share = s, DirectoryItem = i })
            .Where(x => x.DirectoryItem.Uid == uid && x.Share.ExpireDate > now)
            .Select(x => new
            {
                Code = x.Share.Id,
                Expire = new DateTimeOffset(x.Share.ExpireDate).ToUnixTimeMilliseconds(),
                DirectoryItemId = x.DirectoryItem.Id.ToString(),
                x.DirectoryItem.Name,
                x.DirectoryItem.IsFile
            })
            .ToListAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var guid = ToGuid(id);
        _msSqlContext.Shares.Remove(await _msSqlContext.Shares.SingleAsync(s => s.Id == guid));
        await _msSqlContext.SaveChangesAsync();
    }
}