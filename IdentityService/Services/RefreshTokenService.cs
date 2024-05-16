using IdentityService.Controllers;
using Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services
{
    public class RefreshTokenService(MsSqlContext msSqlContext, JwtService jwtService)
    {
        private readonly MsSqlContext _msSqlContext = msSqlContext;
        private readonly JwtService _jwtService = jwtService;

        public async Task<AppResponse> RefreshTokenAsync(TokenData tokenData)
        {
            var token = tokenData.Token;
            var newToken = await _jwtService.RefreshTokenAsync(token, async (payload) =>
            {
                var uid = Convert.ToInt64(payload["uid"]);
                var user = await _msSqlContext.Users.SingleAsync(u => u.Id == uid);
                return user.Enable;
            });
            return newToken is null ?
                AppResponse.RequireLogBackIn() : AppResponse.Success(new { Token = newToken });
        }
    }
}
