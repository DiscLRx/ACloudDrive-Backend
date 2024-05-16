using Grpc.Net.Client;
using IdentityService.Configuration;

namespace IdentityService.Protos.Clients;

public class GrpcRootDirectoryInitializerClient
{
    private readonly RootDirectoryInitializer.RootDirectoryInitializerClient _rootDirectoryInitializerClient;

    public GrpcRootDirectoryInitializerClient(AppConfig appConfig)
    {
        var grpcHostName = appConfig.Services.FileService;
        var channel = GrpcChannel.ForAddress(grpcHostName);
        _rootDirectoryInitializerClient = new RootDirectoryInitializer.RootDirectoryInitializerClient(channel);
    }

    public async Task RootDirectoryInitializerAsync(long uid)
    {
        var createRootArgs = new CreateRootArgs { Uid = uid};
        await _rootDirectoryInitializerClient.CreateRootDirectoryAsync(createRootArgs);
    }
}