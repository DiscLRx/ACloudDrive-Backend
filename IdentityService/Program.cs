global using Infrastructure.Response;
using IdentityService.Configuration;
using IdentityService.Protos.Clients;
using IdentityService.Protos.Services;
using IdentityService.Security;
using IdentityService.Services;
using Infrastructure.Configuration;
using Infrastructure.Databases;
using Infrastructure.Log;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace IdentityService
{
    public class Program
    {
        private const string CorsPolicy = "CorsPolicy";

        private static readonly AppConfig Config = new();

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.Bind(Config);
            builder.Host.UseCustomSerilog(Config.Databases.MSSQL.ConnectionString, TableName.IdentityService);
            builder.Services.AddSingleton(Config);
            builder.Services.AddSystemConfig();
            builder.Services.AddCors(options =>
                options.AddPolicy(name: CorsPolicy, policy =>
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader())
            );
            builder.Services.AddAuthentication(options =>
            {
                var schemeName = typeof(AuthenticationHandler).Name;
                options.AddScheme<AuthenticationHandler>(schemeName, schemeName);
                options.DefaultAuthenticateScheme = schemeName;
                options.DefaultChallengeScheme = schemeName;
                options.DefaultForbidScheme = schemeName;
            });
            builder.Services.AddControllers().AddJsonOptions(options =>
                options.JsonSerializerOptions.IncludeFields = true);
            builder.Services.AddGrpc();
            builder.Services.AddSingleton<GrpcRootDirectoryInitializerClient>();
            builder.Services.AddDbContext<MsSqlContext>(options =>
                options.UseSqlServer(Config.Databases.MSSQL.ConnectionString));
            var redisConfig = Config.Databases.Redis;
            builder.Services.AddSingleton(new RedisContext(redisConfig.Host, redisConfig.Port, redisConfig.Password));
            builder.Services.AddSingleton<EmailService>();
            builder.Services.AddScoped<SignInService>();
            builder.Services.AddScoped<SignUpService>();
            builder.Services.AddScoped<ForgetPasswordService>();
            builder.Services.AddScoped<UserCheckService>();
            builder.Services.AddSingleton<VerificationCodeService>();
            builder.Services.AddSingleton<RefreshTokenService>();
            builder.Services.AddSingleton<JwtService>();
            builder.Services.AddScoped<HttpContextService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();
            app.UseCors(CorsPolicy);
            app.UseAuthentication();
            app.ConfigureUserInfoLog();
            app.UseAuthorization();
            app.MapGrpcService<GrpcAuthenticatorService>();
            app.MapGrpcService<GrpcUpdateSystemConfigService>();
            app.MapControllers();
            app.Run();
        }
    }
}