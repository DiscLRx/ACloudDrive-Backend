using System.Security.Claims;
using System.Text.Json;
using IdentityService.Services;
using Microsoft.AspNetCore.Authentication;

namespace IdentityService.Security;

public class AuthenticationHandler : IAuthenticationHandler
    {
        private AuthenticationScheme? _scheme;
        private HttpContext? _context;
        private bool _canRefresh = true;
        private JwtService _jwtService;

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _scheme = scheme;
            _context = context;
            _jwtService = _context.RequestServices.GetService<JwtService>();
            return Task.CompletedTask;
        }

        public async Task<AuthenticateResult> AuthenticateAsync()
        {
            var token = ParseToken();
            if (token is null)
            {
                _canRefresh = false;
                return AuthenticateResult.Fail("没有令牌");
            }

            var (authPass, canRefresh, uid, role) = await _jwtService.VerifyTokenAsync(token);

            if (!authPass)
            {
                _canRefresh = canRefresh;
                return AuthenticateResult.Fail("令牌验证失败");
            }

            // _context!.Items["uid"] = uid;

            return AuthenticateResult.Success(CreateTicket(uid, role));
        }

        private string? ParseToken()
        {
            var headers = _context!.Request.Headers;
            headers.TryGetValue("Authorization", out var authHeaderValues);
            var authHeader = authHeaderValues.FirstOrDefault()?.Split(' ');

            if ("Bearer" != authHeader?.GetValue(0) as string || authHeader.GetValue(1) is not string token)
            {
                return null;
            }

            return token;
        }

        private AuthenticationTicket CreateTicket(long uid, string role)
        {
            var principal = new ClaimsPrincipal();
            var claimsIdentity = new ClaimsIdentity([
                new Claim(ClaimTypes.Role, role),
                new Claim("uid", uid.ToString())
            ], _scheme!.Name);
            principal.AddIdentity(claimsIdentity);
            return new AuthenticationTicket(principal, _scheme.Name);
        }

        public async Task ChallengeAsync(AuthenticationProperties? properties)
        {
            _context!.Response.StatusCode = 401;
            var res = _canRefresh ? AppResponse.TokenExpired() : AppResponse.RequireLogBackIn();
            await _context.Response.WriteAsJsonAsync(res,
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { IncludeFields = true });
        }

        public async Task ForbidAsync(AuthenticationProperties? properties)
        {
            _context!.Response.StatusCode = 403;
            await _context.Response.WriteAsJsonAsync(AppResponse.Unauthorized(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { IncludeFields = true });
        }
    }