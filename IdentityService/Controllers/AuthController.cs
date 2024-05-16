using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    public record SignInData(string Username, string Password);
    public record TokenData(string Token);

    [ApiController]
    [Route("api/auth")]
    public class AuthController(SignInService signInService, RefreshTokenService refreshTokenService) : ControllerBase
    {
        private readonly SignInService _signInService = signInService;
        private readonly RefreshTokenService _refreshTokenService = refreshTokenService;

        [HttpPost("sign-in")]
        public async Task<AppResponse> SignIn([FromBody] SignInData signInData)
        {
            return await _signInService.SignIn(signInData);
        }

        [HttpPost("refresh-token")]
        public async Task<AppResponse> RefreshToken([FromBody] TokenData tokenData)
        {
            return await _refreshTokenService.RefreshTokenAsync(tokenData);
        }
    }
}
