using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Infrastructure.InventoryCache;

namespace Server.Infrastructure.BackgroundJobs;

public class NightlyInventorySyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NightlyInventorySyncService> _logger;
    private readonly InventoryCacheOptions _options;
    private readonly TimeProvider _timeProvider;

    public NightlyInventorySyncService(
        IServiceProvider serviceProvider,
        IOptions<InventoryCacheOptions> options,
        ILogger<NightlyInventorySyncService> logger,
        TimeProvider? timeProvider = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = _timeProvider.GetUtcNow();
            var nextRun = CalculateNextRun(now);
            var delay = nextRun - now;

            if (delay > TimeSpan.Zero)
            {
                _logger.LogInformation(
                    "Nightly inventory sync scheduled for {NextRun} (in {Delay}).",
                    nextRun,
                    delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunRefreshAsync(stoppingToken);
        }
    }

    private DateTimeOffset CalculateNextRun(DateTimeOffset referenceTime)
    {
        var scheduledTime = new DateTimeOffset(
            referenceTime.Year,
            referenceTime.Month,
            referenceTime.Day,
            0,
            0,
            0,
            TimeSpan.Zero).Add(_options.SyncTimeUtc);

        if (scheduledTime <= referenceTime)
        {
            scheduledTime = scheduledTime.AddDays(1);
        }

        return scheduledTime;
    }

    private async Task RunRefreshAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var refresher = scope.ServiceProvider.GetRequiredService<IInventoryCacheRefresher>();
            await refresher.RefreshAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nightly inventory sync failed.");
        }
    }
}
