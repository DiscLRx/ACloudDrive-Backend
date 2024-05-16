namespace FileService.Utils
{
    public class DirectoryItemNameCheck
    {
        public static bool IsInvalidName(string fileName) =>
            Path.GetInvalidFileNameChars().Any(fileName.Contains);
    }
}
