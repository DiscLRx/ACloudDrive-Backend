using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Utils;

public class HashValue
{
    private static byte[] GetBytes(string data)
    {
        return Encoding.UTF8.GetBytes(data);
    }

    public static async Task<string> Sha256Async(string data)
    {
        return await Sha256Async(GetBytes(data));
    }

    public static async Task<string> Sha256Async(byte[] data)
    {
        return await Sha256Async(new MemoryStream(data));
    }

    public static async Task<string> Sha256Async(Stream stream)
    {
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash).ToLower();
    }
}