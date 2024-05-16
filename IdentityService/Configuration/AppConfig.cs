namespace IdentityService.Configuration;

public class AppConfig
{
    public DatabasesConfig Databases { get; set; } = new();
    public ServicesConfig Services { get; set; } = new();
}

public class DatabasesConfig
{
    public MsSqlConfig MSSQL { get; set; } = new();
    public RedisConfig Redis { get; set; } = new();
}

public class MsSqlConfig
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class RedisConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class ServicesConfig
{
    public string FileService { get; set; } = string.Empty;
}