global using FileService.Utils;
using FileService.Background;
using FileService.Configuration;
using FileService.Protos.Clients;
using FileService.Protos.Services;
using FileService.Security;
using FileService.Services;
using FileService.Services.Data;
using Infrastructure.Configuration;
using Infrastructure.Databases;
using Infrastructure.Log;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Minio;

namespace FileService;

public class Program
{
    private const string CorsPolicy = "CorsPolicy";

    private static readonly AppConfig Config = new();

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.Bind(Config);
        builder.Host.UseCustomSerilog(Config.Databases.MSSQL.ConnectionString, TableName.FileService);
        builder.Services.AddSingleton(Config);
        builder.Services.AddSystemConfig();
        builder.Services.AddCors(options =>
            options.AddPolicy(name: CorsPolicy, policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader())
        );
        builder.Services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.IncludeFields = true);
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<GrpcAuthenticatorClient>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthentication(options =>
        {
            var schemeName = typeof(RemoteAuthenticationHandler).Name;
            options.AddScheme<RemoteAuthenticationHandler>(schemeName, schemeName);
            options.DefaultAuthenticateScheme = schemeName;
            options.DefaultChallengeScheme = schemeName;
            options.DefaultForbidScheme = schemeName;
        });
        builder.Services.AddDbContext<MsSqlContext>(options =>
            options.UseSqlServer(Config.Databases.MSSQL.ConnectionString));
        var redisConfig = Config.Databases.Redis;
        builder.Services.AddSingleton(new RedisContext(redisConfig.Host, redisConfig.Port, redisConfig.Password));
        builder.Services.AddMinio(cfg =>
        {
            cfg.WithEndpoint(Config.MinIO.Endpoint);
            cfg.WithCredentials(Config.MinIO.AccessKey, Config.MinIO.SecretKey);
            cfg.WithSSL(false);
        });
        builder.Services.AddScoped<DirectoryItemDbService>();
        builder.Services.AddScoped<FileDbService>();
        builder.Services.AddScoped<FileHashUploadService>();
        builder.Services.AddScoped<FilePhysicalUploadService>();
        builder.Services.AddScoped<DirectoryItemOperationCheckService>();
        builder.Services.AddScoped<DirectoryItemService>();
        builder.Services.AddScoped<HttpContextService>();
        builder.Services.AddScoped<RecycleBinService>();
        builder.Services.AddScoped<RecycleBinDbService>();
        builder.Services.AddScoped<FileDownloadService>();
        builder.Services.AddScoped<ShareService>();
        builder.Services.AddScoped<ShareDbService>();
        builder.Services.AddScoped<FileManageService>();
        builder.Services.AddHostedService<DeleteUnreferencedFileService>();
        builder.Services.AddHostedService<DeleteExpiredShareService>();

        var app = builder.Build();
        app.UseCors(CorsPolicy);
        app.UseAuthentication();
        app.ConfigureUserInfoLog();
        app.UseAuthorization();
        app.MapGrpcService<GrpcRootDirectoryInitializerService>();
        app.MapGrpcService<GrpcUpdateSystemConfigService>();
        app.MapControllers();
        app.Run();
    }
}