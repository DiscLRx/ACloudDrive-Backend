using Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace FileService.Background;

public class DeleteExpiredRecycleBinService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ExpireTimeSpan = TimeSpan.FromDays(31);

    private readonly MsSqlContext _msSqlContext =
        serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<MsSqlContext>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecDeleteAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ExecDeleteAsync(CancellationToken stoppingToken)
    {
        var finalKeepDate = DateTime.Now - ExpireTimeSpan;
        await _msSqlContext.RecycleBins.Where(r => r.DeleteDate < finalKeepDate).ExecuteDeleteAsync(stoppingToken);
    }
}