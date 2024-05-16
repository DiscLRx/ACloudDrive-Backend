using Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;

namespace FileService.Background;

public class DeleteExpiredShareService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

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
        var now = DateTime.Now;
        await _msSqlContext.Shares.Where(s => s.ExpireDate <= now).ExecuteDeleteAsync(stoppingToken);
    }
}