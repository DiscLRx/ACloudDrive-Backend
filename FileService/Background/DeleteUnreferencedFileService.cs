using FileService.Configuration;
using Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;

namespace FileService.Background;

public class DeleteUnreferencedFileService(
    IServiceScopeFactory serviceScopeFactory,
    IMinioClient minioClient,
    AppConfig appConfig)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly AppConfig _appConfig = appConfig;

    private readonly MsSqlContext _msSqlContext =
        serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<MsSqlContext>();

    private readonly IMinioClient _minioClient = minioClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecDeleteAsync(stoppingToken);
            await SetDeleteLock(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SetDeleteLock(CancellationToken stoppingToken)
    {
        var files = await _msSqlContext.Files
            .Where(f => f.ReferenceCount == 0 && f.DeleteFlag == false)
            .ToArrayAsync(stoppingToken);
        foreach (var file in files)
        {
            file.DeleteFlag = true;
        }

        await _msSqlContext.SaveChangesAsync(stoppingToken);
    }

    private async Task ExecDeleteAsync(CancellationToken stoppingToken)
    {
        var unreferencedObjectNames = await _msSqlContext.Files
            .Where(f => f.DeleteFlag == true)
            .Select(f => f.Id.ToString())
            .AsNoTracking()
            .ToListAsync(stoppingToken);
        var arg = new RemoveObjectsArgs()
            .WithBucket(_appConfig.MinIO.DefaultBucket)
            .WithObjects(unreferencedObjectNames);
        await _msSqlContext.Files.Where(f => f.ReferenceCount == 0)
            .ExecuteDeleteAsync(cancellationToken: stoppingToken);
        _ = _minioClient.RemoveObjectsAsync(arg, stoppingToken);
    }
}