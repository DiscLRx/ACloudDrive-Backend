using Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace FileService.Services.Data
{
    public class DirectoryItemDbService(
        MsSqlContext msSqlContext,
        FileDbService fileDbService)
    {
        private readonly MsSqlContext _msSqlContext = msSqlContext;
        private readonly FileDbService _fileDbService = fileDbService;

        private static Guid ToGuid(string id)
        {
            try
            {
                return new Guid(id);
            }
            catch
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// 根据id获取目录项
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<DirectoryItem?> GetAsync(string id)
        {
            var guid = ToGuid(id);
            return await _msSqlContext.DirectoryItems.SingleOrDefaultAsync(i => i.Id == guid);
        }

        /// <summary>
        /// 根据uid和给定路径获取目录项
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<DirectoryItem?> GetByPathAsync(long uid, string path)
        {
            var item = await GetRootAsync(uid);
            if (path == "")
            {
                return item;
            }
            foreach (var itemName in path.Split('/'))
            {
                item = await _msSqlContext.DirectoryItems
                    .SingleOrDefaultAsync(i => i.ParentId == item.Id && i.Name == itemName);
                if (item is null)
                {
                    return null;
                }
            }
            return item;
        }

        /// <summary>
        /// 获取用户的根目录
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public async Task<DirectoryItem> GetRootAsync(long uid) =>
            await _msSqlContext.DirectoryItems.SingleAsync(i => i.Uid == uid && i.ParentId == null);

        private async Task<List<string>> GetItemPathArrayAsync(string id)
        {
            var pathSegs = new List<string>();
            var guid = ToGuid(id);
            var item = await _msSqlContext.DirectoryItems.SingleAsync(i => i.Id == guid);
            while (item.ParentId is not null)
            {
                pathSegs.Add(item.Name);
                item = await _msSqlContext.DirectoryItems.SingleAsync(i => i.Id == item.ParentId);
            }
            pathSegs.Reverse();
            return pathSegs;
        }

        /// <summary>
        /// 获取目录项的全路径
        /// </summary>
        /// <param name="id">目录项id</param>
        /// <returns></returns>
        public async Task<string> GetPathAsync(string id)
        {
            var pathSegs = await GetItemPathArrayAsync(id);
            return string.Join('/', pathSegs);
        }

        /// <summary>
        /// 获取目录项所在目录的全路径
        /// </summary>
        /// <param name="id">目录项id</param>
        /// <returns></returns>
        public async Task<string> GetDirectorytPathAsync(string id)
        {
            var pathSegs = await GetItemPathArrayAsync(id);
            pathSegs = pathSegs[.. ^1];
            return string.Join('/', pathSegs);
        }

        /// <summary>
        /// 获取指定目录项的子项
        /// </summary>
        /// <param name="parentId">父级目录id</param>
        /// <returns></returns>
        public async Task<List<DirectoryItem>> GetChildrenAsync(string parentId)
        {
            var parentGuid = ToGuid(parentId);
            return await _msSqlContext.DirectoryItems
                .Where(i => i.ParentId == parentGuid)
                .ToListAsync();
        }

        /// <summary>
        /// 获取父级目录项
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<DirectoryItem?> GetParentAsync(string id)
        {
            var item = await GetAsync(id);
            if (item?.ParentId is null)
            {
                return null;
            }
            return await GetAsync(item.ParentId.ToString()!);
        }

        /// <summary>
        /// 获取目录项的所有后代
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<List<DirectoryItem>> GetDescendantsAsync(string id)
        {
            var items = new List<DirectoryItem> { (await GetAsync(id))! };
            var index = 0;
            while (index < items.Count)
            {
                var item = items[index];
                index++;
                if (item.IsFile)
                {
                    continue;
                }
                items.AddRange(await _msSqlContext.DirectoryItems
                    .Where(i => i.ParentId == item.Id).ToListAsync());
            }
            return items;
        }

        /// <summary>
        /// 创建逻辑文件
        /// </summary>
        /// <param name="uid">所属的用户id</param>
        /// <param name="parentId">逻辑目录id</param>
        /// <param name="fileId">文件id</param>
        /// <param name="fileName">文件名称</param>
        public async Task CreateDirectoryFileAsync(long uid, string parentId, string fileId, string fileName) =>
            await CreateDirItemAsync(uid, parentId, fileName, true, fileId);

        /// <summary>
        /// 创建逻辑目录
        /// </summary>
        /// <param name="uid">用户id</param>
        /// <param name="parentId">父级逻辑目录id</param>
        /// <param name="directoryName">目录名称</param>
        public async Task CreateDirectoryAsync(long uid, string parentId, string directoryName) =>
            await CreateDirItemAsync(uid, parentId, directoryName, false);

        /// <summary>
        /// 创建逻辑目录
        /// </summary>
        /// <param name="uid">用户id</param>
        /// <param name="directoryPath">目录路径</param>
        public async Task CreateDirectoryAsync(long uid, string directoryPath)
        {
            var itemNameArray = directoryPath.Split('/');
            var item = await GetRootAsync(uid);
            for (var i = 0; i < itemNameArray.Length; i++)
            {
                var nextItem = await _msSqlContext.DirectoryItems
                    .SingleOrDefaultAsync(di => di.ParentId == item.Id && di.Name == itemNameArray[i]);
                if (nextItem is null)
                {
                    nextItem = new DirectoryItem
                    {
                        ParentId = item.Id,
                        Uid = item.Uid,
                        Name = itemNameArray[i],
                        IsFile = false,
                        FileId = null
                    };
                    await _msSqlContext.DirectoryItems.AddAsync(nextItem);
                }

                item = nextItem;
            }

            await _msSqlContext.SaveChangesAsync();
        }

        /// <summary>
        /// 创建用户根目录
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public async Task CreateRootDirectoryAsync(long uid) =>
            await CreateDirItemAsync(uid, null, "root", false);

        private async Task CreateDirItemAsync(long uid, string? parentId, string name, bool isFile,
            string? fileId = null)
        {
            await _msSqlContext.DirectoryItems.AddAsync(new DirectoryItem
            {
                Uid = uid,
                ParentId = parentId is null ? null : ToGuid(parentId),
                Name = name,
                IsFile = isFile,
                FileId = fileId is null ? null : ToGuid(fileId)
            });
            await _msSqlContext.SaveChangesAsync();
        }

        /// <summary>
        /// 重命名目录项
        /// </summary>
        /// <param name="id">目录项id</param>
        /// <param name="newName">新名称</param>
        public async Task RenameAsync(string id, string newName)
        {
            var guid = ToGuid(id);
            var item = await _msSqlContext.DirectoryItems.SingleAsync(i => i.Id == guid);
            item.Name = newName;
            await _msSqlContext.SaveChangesAsync();
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="targetDirId"></param>
        public async Task MoveAsync(string itemId, string targetDirId)
        {
            var guid = ToGuid(itemId);
            var item = await _msSqlContext.DirectoryItems.SingleAsync(i => i.Id == guid);
            item.ParentId = ToGuid(targetDirId);
            await _msSqlContext.SaveChangesAsync();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="targetDirId"></param>
        public async Task CopyAsync(string itemId, string targetDirId)
        {
            var itemGuid = ToGuid(itemId);
            var targetDir = await GetAsync(targetDirId);
            var uid = targetDir!.Uid;
            var user = await _msSqlContext.Users.SingleAsync(u => u.Id == uid);

            var items = new Queue<DirectoryItem>();

            var item = await _msSqlContext.DirectoryItems.AsNoTracking()
                .SingleAsync(r => r.Id == itemGuid);
            item.ParentId = targetDir.Id;
            items.Enqueue(item);

            do
            {
                item = items.Dequeue();
                var enqItems = await _msSqlContext.DirectoryItems
                    .Where(i => i.ParentId == item.Id)
                    .AsNoTracking()
                    .ToListAsync();
                item.Id = default;
                item.Uid = uid;
                await _msSqlContext.DirectoryItems.AddAsync(item);
                enqItems.ForEach(i =>
                {
                    i.ParentId = item.Id;
                    items.Enqueue(i);
                });

                if (item.IsFile)
                {
                    var fileId = item.FileId.ToString()!;
                    var file = await _fileDbService.IncreaseReferenceCountAsync(fileId);
                    user.UsedSpace += file.FileSize;
                }
            } while (items.Count != 0);

            await _msSqlContext.SaveChangesAsync();
        }

        /// <summary>
        /// 计算目录总大小
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<long> CalculateSizeAsync(string id)
        {
            var items = await GetDescendantsAsync(id);
            return await _msSqlContext.DirectoryItems.Where(i => items.Contains(i))
                .Where(i => i.IsFile)
                .Join(_msSqlContext.Files,
                    i => i.FileId,
                    f => f.Id,
                    (_, f) => new { Size = f.FileSize })
                .SumAsync(x => x.Size);
        }

        /// <summary>
        /// 判断同一父级目录下是否存在是否存在给定名称的目录项
        /// </summary>
        /// <param name="parentId">父级目录id</param>
        /// <param name="name">目录项名称</param>
        public async Task<bool> NameExistsAsync(string parentId, string name)
        {
            var parentGuid = ToGuid(parentId);
            return await _msSqlContext.DirectoryItems
                .AnyAsync(i => i.ParentId == parentGuid && i.Name == name);
        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="searchStr"></param>
        /// <returns></returns>
        public async Task<List<DirectoryItem>> SearchAsync(long uid, string searchStr)
        {
            return await _msSqlContext.DirectoryItems
                .Where(i => i.Uid == uid)
                .Where(i => i.Name.Contains(searchStr))
                .Where(i => i.ParentId != null)
                .ToListAsync();
        }
    }
}