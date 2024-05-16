using IdentityService.Controllers;
using Infrastructure.Databases;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services
{
    public class SignInService(MsSqlContext msSqlContext, JwtService jwtService)
    {
        private readonly MsSqlContext _msSqlContext = msSqlContext;
        private readonly JwtService _jwtService = jwtService;

        public async Task<AppResponse> SignIn(SignInData signInData)
        {
            if (string.IsNullOrWhiteSpace(signInData.Username)
                || string.IsNullOrWhiteSpace(signInData.Password))
            {
                return AppResponse.InvalidArgument();
            }

            var user = await _msSqlContext.Users.Where(u => u.Username == signInData.Username).FirstOrDefaultAsync();
            if (user is null || !user.Enable)
            {
                return AppResponse.SignInFailed();
            }

            if (!PasswordHelper.Verify(user.Password, signInData.Password))
            {
                return AppResponse.SignInFailed();
            }

            return AppResponse.Success(new
            {
                Token = _jwtService.CreateJwt(user.Id, user.Role),
                DisplayName = user.DisplayName,
                Role = user.Role
            });
        }

    }
}
