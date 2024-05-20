using Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace FileService.Services.Data;

public class RecycleBinDbService(MsSqlContext msSqlContext, DirectoryItemDbService directoryItemDbService, FileDbService fileDbService)
{
    private readonly MsSqlContext _msSqlContext = msSqlContext;
    private readonly DirectoryItemDbService _directoryItemDbService = directoryItemDbService;
    private readonly FileDbService _fileDbService = fileDbService;

    private static Guid ToGuid(string id) => new(id);

    public async Task<RecycleBin?> GetAsync(string id)
    {
        var guid = ToGuid(id);
        return await _msSqlContext.RecycleBins.SingleOrDefaultAsync(i => i.Id == guid);
    }

    /// <summary>
    /// 获取指定用户至今一定时间范围内的的所有根回收项
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="timeSpan">时间范围</param>
    /// <returns></returns>
    public async Task<List<RecycleBin>> BrowseAsync(long uid, TimeSpan timeSpan)
    {
        var finalDate = DateTime.Now - timeSpan;
        return await _msSqlContext.RecycleBins
            .Where(i => i.IsRecycleRoot)
            .Where(i => i.Uid == uid)
            .Where(i => i.DeleteDate >= finalDate)
            .ToListAsync();
    }


    public async Task MoveToRecycleBinAsync(string itemId)
    {
        var now = DateTime.Now;
        var guid = ToGuid(itemId);
        var dirItems = new Queue<DirectoryItem>();
        var first = await _msSqlContext.DirectoryItems
            .SingleAsync(i => i.Id == guid);
        dirItems.Enqueue(first);
        do
        {
            var dirItem = dirItems.Dequeue();
            var enqItems = await _msSqlContext.DirectoryItems
                .Where(i => i.ParentId == dirItem.Id).ToListAsync();
            enqItems.ForEach(dirItems.Enqueue);

            var dirPath = await _directoryItemDbService.GetDirectorytPathAsync(itemId);
            await _msSqlContext.RecycleBins.AddAsync(new RecycleBin
            {
                DirItemId = dirItem.Id,
                ParentId = (Guid)dirItem.ParentId!,
                Uid = dirItem.Uid,
                Name = dirItem.Name,
                IsFile = dirItem.IsFile,
                FileId = dirItem.FileId,
                IsRecycleRoot = dirItem == first,
                DirPath = dirPath,
                DeleteDate = now
            });
            _msSqlContext.DirectoryItems.Remove(dirItem);
        } while (dirItems.Count != 0);

        await _msSqlContext.SaveChangesAsync();
    }

    /// <summary>
    /// 还原回收站项目
    /// </summary>
    /// <param name="id">回收站项目id</param>
    /// <returns>如果还原的目标位置存在相同名称的目录项，则返回false</returns>
    public async Task<bool> RestoreAsync(string id)
    {
        var guid = ToGuid(id);
        var recycleItems = new Queue<RecycleBin>();
        var recycleRoot = await _msSqlContext.RecycleBins
            .SingleAsync(r => r.Id == guid);
        recycleItems.Enqueue(recycleRoot);
        var parent = await _directoryItemDbService.GetByPathAsync(recycleRoot.Uid, recycleRoot.DirPath);
        if (parent is null)
        {
            await _directoryItemDbService.CreateDirectoryAsync(recycleRoot.Uid, recycleRoot.DirPath);
            parent = (await _directoryItemDbService.GetByPathAsync(recycleRoot.Uid, recycleRoot.DirPath))!;
            recycleRoot.ParentId = parent.Id;
        }
        else if (await _directoryItemDbService.NameExistsAsync(parent.Id.ToString(), recycleRoot.Name))
        {
            return false;
        }

        do
        {
            var recycleItem = recycleItems.Dequeue();
            var enqItems = await _msSqlContext.RecycleBins
                .Where(i => !i.IsRecycleRoot && i.ParentId == recycleItem.DirItemId)
                .ToListAsync();
            enqItems.ForEach(recycleItems.Enqueue);

            await _msSqlContext.DirectoryItems.AddAsync(new DirectoryItem
            {
                Id = recycleItem.DirItemId,
                Uid = recycleItem.Uid,
                ParentId = recycleItem.ParentId,
                Name = recycleItem.Name,
                IsFile = recycleItem.IsFile,
                FileId = recycleItem.FileId
            });
            _msSqlContext.RecycleBins.Remove(recycleItem);
        } while (recycleItems.Count != 0);

        await _msSqlContext.SaveChangesAsync();
        return true;
    }

    public async Task DeleteForeverAsync(string id)
    {
        var guid = ToGuid(id);
        var recycleItems = new Queue<RecycleBin>();
        var recycleRoot = await _msSqlContext.RecycleBins
            .SingleAsync(r => r.Id == guid);
        var uid = recycleRoot.Uid;
        var user = _msSqlContext.Users.Single(u => u.Id == uid);
        recycleItems.Enqueue(recycleRoot);
        do
        {
            var recycleItem = recycleItems.Dequeue();
            var enqItems = await _msSqlContext.RecycleBins
                .Where(i => !i.IsRecycleRoot && i.ParentId == recycleItem.DirItemId)
                .ToListAsync();
            enqItems.ForEach(recycleItems.Enqueue);

            _msSqlContext.RecycleBins.Remove(recycleItem);
            if (recycleItem.IsFile)
            {
                var fileId = recycleItem.FileId.ToString()!;
                await _fileDbService.DecreaseReferenceCountAsync(fileId);
                var fileSize = await _fileDbService.GetFileSizeAsync(fileId);
                user.UsedSpace -= fileSize;
            }
        } while (recycleItems.Count != 0);

        await _msSqlContext.SaveChangesAsync();
    }
}