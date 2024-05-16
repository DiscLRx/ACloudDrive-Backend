using Serilog.Context;

namespace Infrastructure.Log;

public static class LogUserInfoExtensions
{
    public static IApplicationBuilder ConfigureUserInfoLog(this IApplicationBuilder builder)
    {
        UserInfoEnricher.ServiceScopeFactory = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
        return builder;
    }
}
