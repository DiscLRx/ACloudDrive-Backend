using Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using File = Infrastructure.Databases.File;

namespace FileService.Services.Data
{
    public class FileDbService(MsSqlContext msSqlContext)
    {
        private readonly MsSqlContext _msSqlContext = msSqlContext;

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
        /// 根据id获取文件
        /// </summary>
        /// <param name="id">文件id</param>
        /// <returns></returns>
        public async Task<File?> GetFileAsync(string id)
        {
            return await _msSqlContext.Files.SingleOrDefaultAsync(f => f.Id == ToGuid(id));
        }

        /// <summary>
        /// 根据哈希获取文件
        /// </summary>
        /// <param name="headHash">头部哈希</param>
        /// <param name="entiretyHash">整体哈希</param>
        /// <returns></returns>
        public async Task<File?> GetFileAsync(string headHash, string entiretyHash)
        {
            return await _msSqlContext.Files.SingleOrDefaultAsync(f =>
                f.HeadHash == headHash && f.EntiretyHash == entiretyHash);
        }

        /// <summary>
        /// 获取所有文件
        /// </summary>
        /// <returns></returns>
        public async Task<List<File>> GetFilesAsync()
        {
            return await _msSqlContext.Files.ToListAsync();
        }

        /// <summary>
        /// 根据哈希判断文件是否存在
        /// </summary>
        /// <param name="headHash">首部哈希</param>
        /// <param name="entiretyHash">整体哈希</param>
        /// <returns></returns>
        public async Task<bool> FileExistsAsync(string headHash, string entiretyHash)
        {
            return await _msSqlContext.Files.Where(f =>
                f.HeadHash == headHash && f.EntiretyHash == entiretyHash).AnyAsync();
        }

        /// <summary>
        /// 获取指定文件的大小
        /// </summary>
        /// <param name="id">文件id</param>
        /// <returns></returns>
        public async Task<long> GetFileSizeAsync(string id)
        {
            var guid = ToGuid(id);
            return await _msSqlContext.Files.Where(f => f.Id == guid).Select(f => f.FileSize).SingleAsync();
        }

        /// <summary>
        /// 增加文件引用计数
        /// </summary>
        /// <param name="id">文件id</param>
        /// <returns></returns>
        public async Task<File> IncreaseReferenceCountAsync(string id)
        {
            var guid = ToGuid(id);
            try
            {
                var file = await _msSqlContext.Files.SingleAsync(f => f.Id == guid);

                if (file.DeleteFlag)
                {
                    await _msSqlContext.Files.Where(f => f.Id == file.Id).ExecuteDeleteAsync();
                    file.DeleteFlag = false;
                    file.ReferenceCount = 1;
                    await _msSqlContext.Files.AddAsync(file);
                    await _msSqlContext.SaveChangesAsync();
                }

                file.ReferenceCount++;
                await _msSqlContext.SaveChangesAsync();
                return file;
            }
            catch (DbUpdateException)
            {
                return await IncreaseReferenceCountAsync(id);
            }
        }

        /// <summary>
        /// 减少文件引用计数
        /// </summary>
        /// <param name="id">文件id</param>
        /// <returns></returns>
        public async Task<File> DecreaseReferenceCountAsync(string id)
        {
            try
            {
                var guid = ToGuid(id);
                var file = await _msSqlContext.Files.SingleOrDefaultAsync(f => f.Id == guid);
                file!.ReferenceCount--;
                await _msSqlContext.SaveChangesAsync();
                return file;
            }
            catch (DbUpdateConcurrencyException)
            {
                return await DecreaseReferenceCountAsync(id);
            }
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        public async Task CreateFileAsync(File file)
        {
            await _msSqlContext.Files.AddAsync(file);
            await _msSqlContext.SaveChangesAsync();
        }

        /// <summary>
        /// 设置文件上传锁状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="locking"></param>
        /// <returns></returns>
        public async Task SetFileUploadingLock(string id, bool locking)
        {
            var file = await _msSqlContext.Files.SingleAsync(f => f.Id == ToGuid(id));
            file.UploadingLock = locking;
            await _msSqlContext.SaveChangesAsync();
        }

        /// <summary>
        /// 设置文件可用性
        /// </summary>
        /// <param name="id"></param>
        /// <param name="enable"></param>
        public async Task SetFileEnableAsync(string id, bool enable)
        {
            var file = await _msSqlContext.Files.SingleAsync(f => f.Id == ToGuid(id));
            file.Enable = enable;
            await _msSqlContext.SaveChangesAsync();
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="id">文件id</param>
        /// <returns></returns>
        public async Task<bool> DeleteFileAsync(string id)
        {
            var file = await _msSqlContext.Files.SingleOrDefaultAsync(f => f.Id == ToGuid(id));
            if (file is null || file.ReferenceCount > 0)
            {
                return false;
            }

            _msSqlContext.Files.Remove(file);
            await _msSqlContext.SaveChangesAsync();
            return true;
        }
    }
}