using System.Data;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace Infrastructure.Log;

public enum TableName
{
    FileService,
    SystemService,
    IdentityService
}

internal static class TableNameExtension
{
    internal static string GetTableName(this TableName tableName)
    {
        return tableName switch
        {
            TableName.FileService => "file_service_log",
            TableName.SystemService => "system_service_log",
            TableName.IdentityService => "identity_service_log",
        };
    }
}

public class UserInfoEnricher : ILogEventEnricher
{
    public static IServiceScopeFactory ServiceScopeFactory;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContextAccessor = ServiceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor?.HttpContext;
        var uidStr = httpContext?.User.Claims.SingleOrDefault(c => c.Type == "uid")?.Value;
        long? uid = uidStr is null ? null : Convert.ToInt64(uidStr);
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("operator_id", uid));
    }
}


public static class EnricherExtensions
{
    public static LoggerConfiguration WithUserInfo(this LoggerEnrichmentConfiguration enrich)
    {
        return enrich.With(new UserInfoEnricher());
    }
}

public static class SerilogExtension
{
    public static IHostBuilder UseCustomSerilog(this ConfigureHostBuilder host, string connectionString, TableName tableName)
    {
        var logColumnOptions = new ColumnOptions
        {
            Id = { ColumnName = "id", DataType = SqlDbType.BigInt},
            Message = { ColumnName = "message" },
            Level = { ColumnName = "level" },
            TimeStamp = { ColumnName = "date", DataType = SqlDbType.DateTime2 },
            Exception = { ColumnName = "exception" },
            TraceId = { ColumnName = "trace_id", DataType = SqlDbType.VarChar, DataLength = 40}
        };
        logColumnOptions.Store = logColumnOptions.Store
            .Where(s => s != StandardColumn.MessageTemplate)
            .Where(s => s != StandardColumn.Properties)
            .ToList();
        logColumnOptions.Store.Add(StandardColumn.TraceId);

        var operatorIdColumn = new SqlColumn("operator_id", SqlDbType.BigInt);
        logColumnOptions.AdditionalColumns = [operatorIdColumn];

        return host.UseSerilog(
            new LoggerConfiguration()
                .Enrich.WithUserInfo()
                .WriteTo.MSSqlServer(
                    connectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        SchemaName = "a_cloud_drive",
                        TableName = tableName.GetTableName()
                    },
                    columnOptions: logColumnOptions)
                .WriteTo.Console()
                .CreateLogger()
        );
    }
}