using System.Text;
using System.Text.Json;
using Infrastructure.Databases;
using Infrastructure.Utils;
using SystemConfig = Infrastructure.Configuration.SystemConfig;

namespace IdentityService.Services
{
    public class VerificationCodeService(RedisContext redisContext, EmailService emailService, SystemConfig systemConfig, IServiceProvider serviceProvider)
    {
        private record VerificationCodeCache(string Key, string Code);

        private readonly RedisContext _redisContext = redisContext;
        private readonly EmailService _emailService = emailService;
        private readonly SystemConfig _systemConfig = systemConfig;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        private static class VerificationCodeGenerator
        {
            private static readonly Random _random = new();
            private static string RandomHexString() => _random.NextInt64(0x10_000_000_000_000, 0xFF_FFF_FFF_FFF_FFF).ToString("X2");
            public static string GenerateKey()
            {
                var builder = new StringBuilder();
                for (var i = 0; i < 3; i++)
                {
                    builder.Append(RandomHexString());
                }
                return builder.ToString();
            }
            public static string GenerateCode() => _random.Next(100_000, 999_999).ToString();
        }

        private async Task SendVerificationCodeAsync(string email, string code)
        {
            using var scoped = _serviceProvider.CreateScope();
            const string subject = "网络文件管理系统";
            var time = TimeConvert.SecondsToString(_systemConfig.VerificationCodeExpireSeconds,
                TimeConvert.Granularity.Minute);
            var body = $"您的验证码是: {code}， 验证码有效期为{time}。";
            await _emailService.SendMailAsync(email, subject, body);
        }

        /// <summary>
        /// 发送邮件验证码
        /// </summary>
        /// <param name="email"></param>
        public async Task<string?> MakeVerificationCodeAsync(string email)
        {
            var vKey = VerificationCodeGenerator.GenerateKey();
            var vCode = VerificationCodeGenerator.GenerateCode();
            var expireTimeSpan = TimeSpan.FromSeconds(_systemConfig.VerificationCodeExpireSeconds);
            await _redisContext.VerificationCode.StringSetAsync(
                email,
                JsonSerializer.Serialize(new VerificationCodeCache(vKey, vCode)),
                expireTimeSpan);
            _ = SendVerificationCodeAsync(email, vCode);
            return vKey;
        }

        public async Task<bool> VerifyCodeAsync(string email, string key, string code)
        {
            string? cacheStr = await _redisContext.VerificationCode.StringGetAsync(email);
            if (cacheStr is null)
            {
                return false;
            }

            var cache = JsonSerializer.Deserialize<VerificationCodeCache>(cacheStr);
            if ((cache?.Key != key) || (cache?.Code != code))
            {
                return false;
            }
            _ = _redisContext.VerificationCode.KeyDeleteAsync(email);
            return true;

        }
    }
}
