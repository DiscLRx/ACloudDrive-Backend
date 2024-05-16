namespace FileService.Utils;

public class FileHelper
{
    /// <summary>
    /// 创建一个指定大小的预填充文件
    /// </summary>
    /// <param name="fileSize"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static async Task<string> CreatePreFillFile(long fileSize ,string? fileName = null)
    {
        fileName ??= Path.GetTempFileName();
        await using var fs = File.OpenWrite(fileName);
        fs.Seek(fileSize - 1, SeekOrigin.Begin);
        fs.WriteByte(0);
        fs.Close();
        return fileName;
    }
}