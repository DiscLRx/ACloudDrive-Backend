using System.Text.Json.Serialization;
using Infrastructure.Databases;

namespace Infrastructure.Configuration;

public class SystemConfig
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private MsSqlContext _msSqlContext;

    private string _smtpServerHost;

    public string SmtpServerHost
    {
        get => _smtpServerHost;
        set
        {
            _smtpServerHost = value;
            _msSqlContext.SystemConfigs.Single(cfg
                => cfg.ConfigKey == nameof(SmtpServerHost)).ConfigValue = value;
            _msSqlContext.SaveChanges();
        }
    }

    private int _smtpServerPort;

    public int SmtpServerPort
    {
        get => _smtpServerPort;
        set
        {
            _smtpServerPort = value;
            _msSqlContext.SystemConfigs.Single(cfg =>
                    cfg.ConfigKey == nameof(SmtpServerPort)).ConfigValue =
                value.ToString();
            _msSqlContext.SaveChanges();
        }
    }

    private string _emailAccount;

    public string EmailAccount
    {
        get => _emailAccount;
        set
        {
            _emailAccount = value;
            _msSqlContext.SystemConfigs.Single(cfg =>
                cfg.ConfigKey == nameof(EmailAccount)).ConfigValue = value;
            _msSqlContext.SaveChanges();
        }
    }

    private string _emailPassword;

    [JsonIgnore]
    public string EmailPassword
    {
        get => _emailPassword;
        set
        {
            _emailPassword = value;
            _msSqlContext.SystemConfigs.Single(cfg =>
                cfg.ConfigKey == nameof(EmailPassword)).ConfigValue = value;
            _msSqlContext.SaveChanges();
        }
    }

    private long _userDefaultSpace;

    public long UserDefaultSpace
    {
        get => _userDefaultSpace;
        set
        {
            _userDefaultSpace = value;
            _msSqlContext.SystemConfigs.Single(cfg =>
                cfg.ConfigKey == nameof(UserDefaultSpace)).ConfigValue = value.ToString();
            _msSqlContext.SaveChanges();
        }
    }

    private long _tokenExpireSeconds;

    public long TokenExpireSeconds
    {
        get => _tokenExpireSeconds;
        set
        {
            _tokenExpireSeconds = value;
            _msSqlContext.SystemConfigs.Single(cfg =>
                cfg.ConfigKey == nameof(TokenExpireSeconds)).ConfigValue = value.ToString();
            _msSqlContext.SaveChanges();
        }
    }

    private long _tokenRefreshSeconds;

    public long TokenRefreshSeconds
    {
        get => _tokenRefreshSeconds;
        set
        {
            _tokenRefreshSeconds = value;
            _msSqlContext.SystemConfigs.Single(cfg =>
                cfg.ConfigKey == nameof(TokenRefreshSeconds)).ConfigValue = value.ToString();
            _msSqlContext.SaveChanges();
        }
    }

    private long _verificationCodeExpireSeconds;

    public long VerificationCodeExpireSeconds
    {
        get => _verificationCodeExpireSeconds;
        set
        {
            _verificationCodeExpireSeconds = value;
            _msSqlContext.SystemConfigs.Single(cfg =>
                cfg.ConfigKey == nameof(VerificationCodeExpireSeconds)).ConfigValue = value.ToString();
            _msSqlContext.SaveChanges();
        }
    }

    public SystemConfig(MsSqlContext msSqlContext, IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        Refresh();
    }

    public void Refresh()
    {
        _msSqlContext = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<MsSqlContext>();
        var configs = _msSqlContext.SystemConfigs
            .ToDictionary(cfg => cfg.ConfigKey, cfg => cfg.ConfigValue);
        _smtpServerHost = configs[nameof(SmtpServerHost)];
        _smtpServerPort = Convert.ToInt32(configs[nameof(SmtpServerPort)]);
        _emailAccount = configs[nameof(EmailAccount)];
        _emailPassword = configs[nameof(EmailPassword)];
        _userDefaultSpace = Convert.ToInt64(configs[nameof(UserDefaultSpace)]);
        _tokenExpireSeconds = Convert.ToInt64(configs[nameof(TokenExpireSeconds)]);
        _tokenRefreshSeconds = Convert.ToInt64(configs[nameof(TokenRefreshSeconds)]);
        _verificationCodeExpireSeconds = Convert.ToInt64(configs[nameof(VerificationCodeExpireSeconds)]);
    }
}

public static class SystemConfigExtension
{
    public static IServiceCollection AddSystemConfig(this IServiceCollection serviceCollection) =>
        serviceCollection.AddSingleton<SystemConfig>();
}