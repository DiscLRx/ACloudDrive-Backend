using AdminService.Protos.Clients;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using SystemService.Configuration;

namespace SystemService.Protos.Clients;

public class GrpcUpdateSystemConfigClient(AppConfig appConfig)
{

    private readonly List<SystemUpdater.SystemUpdaterClient> _updaterClients =
    [
        new SystemUpdater.SystemUpdaterClient(GrpcChannel.ForAddress(appConfig.Services.IdentityService)),
        new SystemUpdater.SystemUpdaterClient(GrpcChannel.ForAddress(appConfig.Services.FileService))
    ];

    public void UpdateSystemConfig()
    {
        foreach (var updaterClient in _updaterClients)
        {
           _ = updaterClient.UpdateAsync(new Empty());
        }
    }
}