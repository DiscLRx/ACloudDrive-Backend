using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Services;

public class JwtService(SystemConfig systemConfig)
{
    private readonly ECDsaSecurityKey _randomSecurityKey = new(ECDsa.Create());
    private const string Issuer = "ACloudDrive";
    private readonly SystemConfig _systemConfig = systemConfig;

    /// <summary>
    /// 生成一条Json Web Token
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <param name="uid">用户id</param>
    /// <returns></returns>
    public string CreateJwt(long uid, string role)
    {
        var credential = new SigningCredentials(_randomSecurityKey, SecurityAlgorithms.EcdsaSha512);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new("uid", uid.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            claims: claims,
            signingCredentials: credential);
        token.Payload["iat"] = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, TimeZoneInfo.Local).ToUnixTimeSeconds();

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 验证令牌，但不验证有效期
    /// </summary>
    /// <param name="token">令牌</param>
    private async Task<bool> ValidateTokenAsync(string token)
    {
        var jwtHandler = new JwtSecurityTokenHandler();
        var validateResult = await jwtHandler.ValidateTokenAsync(token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = false,
                IssuerSigningKey = _randomSecurityKey,
                ValidateLifetime = false, // 不验证过期时间
                ClockSkew = TimeSpan.Zero
            });
        return validateResult.IsValid;
    }

    /// <summary>
    /// 验证令牌是否在刷新有效期内
    /// </summary>
    /// <param name="payload"></param>
    /// <returns></returns>
    private bool IsBeforeTokenExpire(JwtPayload payload)
    {
        var issuedAt = TimeZoneInfo.ConvertTimeFromUtc(payload.IssuedAt, TimeZoneInfo.Local);
        var expireTime = issuedAt + TimeSpan.FromSeconds(_systemConfig.TokenExpireSeconds);
        return TimeZoneInfo.ConvertTime(DateTimeOffset.Now, TimeZoneInfo.Local).DateTime < expireTime;
    }

    /// <summary>
    /// 验证令牌是否在刷新有效期内
    /// </summary>
    /// <param name="payload"></param>
    /// <returns></returns>
    private bool IsBeforeRefreshTimeLimit(JwtPayload payload)
    {
        var tokenExpireTime = TimeZoneInfo.ConvertTimeFromUtc(payload.IssuedAt, TimeZoneInfo.Local) + TimeSpan.FromSeconds(_systemConfig.TokenExpireSeconds);
        var finalRefreshTime = tokenExpireTime + TimeSpan.FromSeconds(_systemConfig.TokenRefreshSeconds);
        return TimeZoneInfo.ConvertTime(DateTimeOffset.Now, TimeZoneInfo.Local).DateTime < finalRefreshTime;
    }

    /// <summary>
    /// 验证令牌是否有效
    /// </summary>
    /// <param name="token">令牌</param>
    /// <returns></returns>
    public async Task<(bool, bool, long, string)> VerifyTokenAsync(string token)
    {
        var isValid = await ValidateTokenAsync(token);
        JwtPayload payload;
        try
        {
            payload = new JwtSecurityTokenHandler().ReadJwtToken(token).Payload;
            isValid = isValid && IsBeforeTokenExpire(payload);
        }
        catch
        {
            return (false, false, -1, "");
        }

        if (!isValid)
        {
            var canRefresh = IsBeforeRefreshTimeLimit(payload);
            return (false, canRefresh, -1, "");
        }

        var uid = Convert.ToInt64(payload["uid"]);
        var role = payload[ClaimTypes.Role] as string ?? "";
        return (true, true, uid, role);
    }

    /// <summary>
    /// 刷新有效期内的JWT
    /// </summary>
    /// <param name="token"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    public async Task<string?> RefreshTokenAsync(string token, Func<JwtPayload, Task<bool>>? condition = null)
    {
        var isValid = await ValidateTokenAsync(token);
        if (!isValid)
        {
            return null;
        }

        JwtPayload payload;
        try
        {
            payload = new JwtSecurityTokenHandler().ReadJwtToken(token).Payload;
            if (!IsBeforeRefreshTimeLimit(payload))
            {
                return null;
            }
        }
        catch
        {
            return null;
        }

        if (condition is not null && !await condition(payload))
        {
            return null;
        }

        var uid = Convert.ToInt64(payload["uid"]);
        var role = payload[ClaimTypes.Role] as string ?? "";
        return CreateJwt(uid, role);
    }
}