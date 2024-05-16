using AdminService.Protos.Clients;
using Grpc.Net.Client;
using SystemService.Configuration;

namespace SystemService.Protos.Clients;

public class GrpcAuthenticatorClient
{
    private readonly Authenticator.AuthenticatorClient _authenticatorClient;

    public GrpcAuthenticatorClient(AppConfig appConfig)
    {
        var grpcHostName = appConfig.Services.IdentityService;
        var channel = GrpcChannel.ForAddress(grpcHostName);
        _authenticatorClient = new Authenticator.AuthenticatorClient(channel);
    }

    public async Task<AuthResult> AuthenticateAsync(string token)
    {
        var payload = new AuthPayload { Token = token };
        return await _authenticatorClient.AuthenticateAsync(payload);
    }
}