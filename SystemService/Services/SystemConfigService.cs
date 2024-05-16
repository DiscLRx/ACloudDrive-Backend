using Infrastructure.Configuration;
using Infrastructure.Response;
using SystemService.Protos.Clients;

namespace SystemService.Services;

public class SystemConfigService(SystemConfig systemConfig, GrpcUpdateSystemConfigClient grpcUpdateSystemConfigClient)
{
    private readonly SystemConfig _systemConfig = systemConfig;
    private readonly GrpcUpdateSystemConfigClient _grpcUpdateSystemConfigClient = grpcUpdateSystemConfigClient;

    public AppResponse GetSystemConfig()
    {
        return AppResponse.Success(_systemConfig);
    }

    public AppResponse UpdateSystemConfig(string key, string value)
    {
        try
        {
            switch (key)
            {
                case nameof(SystemConfig.SmtpServerHost):
                    _systemConfig.SmtpServerHost = value;
                    break;
                case nameof(SystemConfig.SmtpServerPort):
                    _systemConfig.SmtpServerPort = Convert.ToInt32(value);
                    break;
                case nameof(SystemConfig.EmailAccount):
                    _systemConfig.EmailAccount = value;
                    break;
                case nameof(SystemConfig.EmailPassword):
                    _systemConfig.EmailPassword = value;
                    break;
                case nameof(SystemConfig.UserDefaultSpace):
                    _systemConfig.UserDefaultSpace = Convert.ToInt64(value);
                    break;
                case nameof(SystemConfig.TokenExpireSeconds):
                    _systemConfig.TokenExpireSeconds = Convert.ToInt64(value);
                    break;
                case nameof(SystemConfig.TokenRefreshSeconds):
                    _systemConfig.TokenRefreshSeconds = Convert.ToInt64(value);
                    break;
                case nameof(SystemConfig.VerificationCodeExpireSeconds):
                    _systemConfig.VerificationCodeExpireSeconds = Convert.ToInt64(value);
                    break;
            }
        }
        catch (FormatException)
        {
            return AppResponse.InvalidArgument();
        }
        catch (OverflowException)
        {
            return AppResponse.InvalidArgument();
        }

        _grpcUpdateSystemConfigClient.UpdateSystemConfig();
        return AppResponse.Success();
    }
}