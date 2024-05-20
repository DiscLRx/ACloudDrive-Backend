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
                {
                    var numValue = Convert.ToInt64(value);
                    if (numValue < 0)
                    {
                        throw new FormatException();
                    }
                    _systemConfig.UserDefaultSpace = numValue;
                    break;
                }
                case nameof(SystemConfig.TokenExpireSeconds):
                {
                    var numValue = Convert.ToInt64(value);
                    if (numValue < 0)
                    {
                        throw new FormatException();
                    }
                    _systemConfig.TokenExpireSeconds = numValue;
                    break;
                }
                case nameof(SystemConfig.TokenRefreshSeconds):
                {
                    var numValue = Convert.ToInt64(value);
                    if (numValue < 0)
                    {
                        throw new FormatException();
                    }
                    _systemConfig.TokenRefreshSeconds = numValue;
                    break;
                }
                case nameof(SystemConfig.VerificationCodeExpireSeconds):
                {
                    var numValue = Convert.ToInt64(value);
                    if (numValue < 0)
                    {
                        throw new FormatException();
                    }
                    _systemConfig.VerificationCodeExpireSeconds = numValue;
                    break;
                }
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