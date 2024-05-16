using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Infrastructure.Utils
{
    public class PasswordHelper
    {

        private const int DegreeOfParallelism = 2;
        private const int MemorySize = 2048;
        private const int Iterations = 40;
        private const int SaltLength = 128;
        private const int HashLength = 512;

        public static byte[] Create(string password)
        {
            var salt = new byte[SaltLength];
            RandomNumberGenerator.Create().GetBytes(salt);

            var passwordHash = Encrypt(password, salt);

            var resultPassword = new byte[SaltLength + HashLength];
            salt.CopyTo(resultPassword, 0);
            passwordHash.CopyTo(resultPassword, salt.Length);

            return resultPassword;
        }

        public static bool Verify(byte[] correctPassword, string passwordToVerify)
        {
            var salt = new byte[SaltLength];
            Array.Copy(correctPassword, 0, salt, 0, SaltLength);

            var passwordHash = Encrypt(passwordToVerify, salt);

            var targetHash = new byte[HashLength];
            Array.Copy(correctPassword, SaltLength, targetHash, 0, HashLength);

            return Enumerable.SequenceEqual(passwordHash, targetHash);
        }

        private static byte[] Encrypt(string password, byte[] salt)
        {
            var passwordBytes = Encoding.Default.GetBytes(password);
            var argon2 = new Argon2id(passwordBytes)
            {
                DegreeOfParallelism = DegreeOfParallelism,
                MemorySize = MemorySize,
                Iterations = Iterations,
                Salt = salt
            };

            return argon2.GetBytes(HashLength);
        }

    }
}
