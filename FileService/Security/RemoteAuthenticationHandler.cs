using System.Security.Claims;
using System.Text.Json;
using FileService.Protos.Clients;
using Infrastructure.Response;
using Microsoft.AspNetCore.Authentication;
using UserService.Protos.Clients;

namespace FileService.Security
{
    public class RemoteAuthenticationHandler : IAuthenticationHandler
    {
        private AuthenticationScheme? _scheme;
        private HttpContext? _context;
        private GrpcAuthenticatorClient _grpcAuthenticatorClient;
        private bool _canRefresh = true;

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _scheme = scheme;
            _context = context;
            _grpcAuthenticatorClient =
                context.RequestServices.GetService(typeof(GrpcAuthenticatorClient)) as GrpcAuthenticatorClient
                ?? throw new InvalidOperationException("无法获取身份验证服务");
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

            var result = await _grpcAuthenticatorClient.AuthenticateAsync(token);

            if (!result.AuthPass)
            {
                _canRefresh = result.CanRefresh;
                return AuthenticateResult.Fail("令牌验证失败");
            }

            // var uid = result.Uid;
            // _context!.Items["uid"] = uid;

            return AuthenticateResult.Success(CreateTicket(result));
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

        private AuthenticationTicket CreateTicket(AuthResult result)
        {
            var principal = new ClaimsPrincipal();
            var claimsIdentity = new ClaimsIdentity([
                new Claim(ClaimTypes.Role, result.Role),
                new Claim("uid", result.Uid.ToString())
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
}