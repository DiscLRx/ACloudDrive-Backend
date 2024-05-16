using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Infrastructure.Configuration;

namespace IdentityService.Protos.Services;

public class GrpcUpdateSystemConfigService(SystemConfig systemConfig) : SystemUpdater.SystemUpdaterBase
{
    private readonly SystemConfig _systemConfig = systemConfig;

    public override Task<Empty> Update(Empty request, ServerCallContext context)
    {
        _systemConfig.Refresh();
        return Task.FromResult(new Empty());
    }
}