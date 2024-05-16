using Grpc.Core;
using IdentityService.Services;

namespace IdentityService.Protos.Services;

public class GrpcAuthenticatorService(JwtService jwtService) : Authenticator.AuthenticatorBase
{
    private readonly JwtService _jwtService = jwtService;

    public override async Task<AuthResult> Authenticate(AuthPayload authPayload, ServerCallContext context)
    {
        var (authPass, canRefresh, uid, role) = await _jwtService.VerifyTokenAsync(authPayload.Token);
        return new AuthResult
        {
            AuthPass = authPass,
            CanRefresh = canRefresh,
            Uid = uid,
            Role = role
        };
    }
}