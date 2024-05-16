using Infrastructure.Databases;
using Infrastructure.Log;
using Infrastructure.Response;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;

namespace SystemService.Services;

public class LogService(MsSqlContext msSqlContext)
{
    private readonly MsSqlContext _msSqlContext = msSqlContext;

    public async Task<AppResponse> BrowseLogsAsync(long beginTs, long endTs, string source)
    {
        if (beginTs > endTs)
        {
            return AppResponse.InvalidArgument("开始时间不能晚于结束时间");
        }

        var beginDate = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(beginTs), TimeZoneInfo.Local).DateTime;
        var endDate = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(endTs), TimeZoneInfo.Local).DateTime;
        var logs = source switch
        {
            "file_service" => await _msSqlContext.FileServiceLogs
                .Where((Log l) => beginDate <= l.Date && l.Date <= endDate)
                .ToListAsync(),
            "system_service" => await _msSqlContext.SystemServiceLogs
                .Where((Log l) => beginDate <= l.Date && l.Date <= endDate)
                .ToListAsync(),
            "identity_service" => await _msSqlContext.IdentityServiceLogs
                .Where((Log l) => beginDate <= l.Date && l.Date <= endDate)
                .ToListAsync(),
            _ => []
        };
        return AppResponse.Success(logs);
    }
}