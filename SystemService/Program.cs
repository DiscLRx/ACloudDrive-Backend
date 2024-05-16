using Infrastructure.Configuration;
using Infrastructure.Databases;
using Infrastructure.Log;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Minio;
using SystemService.Configuration;
using SystemService.Protos.Clients;
using SystemService.Security;
using SystemService.Services;

namespace SystemService;

public class Program
{
    private const string CorsPolicy = "CorsPolicy";

    private static readonly AppConfig Config = new();

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.Bind(Config);
        builder.Host.UseCustomSerilog(Config.Databases.MSSQL.ConnectionString, TableName.SystemService);
        builder.Services.AddSingleton(Config);
        builder.Services.AddSystemConfig();
        builder.Services.AddCors(options =>
            options.AddPolicy(name: CorsPolicy, policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader())
        );
        builder.Services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.IncludeFields = true);
        builder.Services.AddMinio(cfg =>
        {
            cfg.WithEndpoint(Config.MinIO.Endpoint);
            cfg.WithCredentials(Config.MinIO.AccessKey, Config.MinIO.SecretKey);
            cfg.WithSSL(false);
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthentication(options =>
        {
            var schemeName = typeof(RemoteAuthenticationHandler<>).Name;
            options.AddScheme<RemoteAuthenticationHandler>(schemeName, schemeName);
            options.DefaultAuthenticateScheme = schemeName;
            options.DefaultChallengeScheme = schemeName;
            options.DefaultForbidScheme = schemeName;
        });
        builder.Services.AddDbContext<MsSqlContext>(options =>
            options.UseSqlServer(Config.Databases.MSSQL.ConnectionString));
        var redisConfig = Config.Databases.Redis;
        builder.Services.AddSingleton(new RedisContext(redisConfig.Host, redisConfig.Port, redisConfig.Password));
        builder.Services.AddSingleton<GrpcAuthenticatorClient>();
        builder.Services.AddSingleton<GrpcUpdateSystemConfigClient>();
        builder.Services.AddScoped<LogService>();
        builder.Services.AddSingleton<SystemConfigService>();

        var app = builder.Build();
        app.UseCors(CorsPolicy);
        app.UseAuthentication();
        app.ConfigureUserInfoLog();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}