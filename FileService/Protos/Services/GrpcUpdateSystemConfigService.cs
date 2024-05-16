using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Infrastructure.Configuration;
using UserService.Protos.Services;

namespace FileService.Protos.Services;

public class GrpcUpdateSystemConfigService(SystemConfig systemConfig) : SystemUpdater.SystemUpdaterBase
{
    private readonly SystemConfig _systemConfig = systemConfig;

    public override Task<Empty> Update(Empty request, ServerCallContext context)
    {
        _systemConfig.Refresh();
        return Task.FromResult(new Empty());
    }
}