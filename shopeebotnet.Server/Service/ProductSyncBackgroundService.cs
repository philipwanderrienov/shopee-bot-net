using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using shopeebotnet.Server.Configs;

namespace shopeebotnet.Server.Service;

public sealed class ProductSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ProductSyncOptions> _options;
    private readonly ILogger<ProductSyncBackgroundService> _logger;

    public ProductSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ProductSyncOptions> options,
        ILogger<ProductSyncBackgroundService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = _options.Value;

        if (!opt.Enabled)
        {
            _logger.LogInformation("Product sync scheduler disabled (ProductSync:Enabled=false).");
            return;
        }

        var interval = Math.Max(1, opt.IntervalMinutes);
        _logger.LogInformation(
            "Product sync scheduler started. IntervalMinutes={IntervalMinutes}, NetworkName={NetworkName}",
            interval,
            opt.NetworkName
        );

        await RunOnceSafeAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // normal shutdown
            }

            if (stoppingToken.IsCancellationRequested) break;
            await RunOnceSafeAsync(stoppingToken);
        }
    }

    private async Task RunOnceSafeAsync(CancellationToken stoppingToken)
    {
        ProductSyncResult result;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IProductSyncService>();
            result = await syncService.SyncOnceAsync(_options.Value, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product sync failed with unhandled exception.");
            return;
        }

        if (!result.Ran)
        {
            _logger.LogInformation("Product sync skipped: {Message}", result.Message);
            return;
        }

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "Product sync completed. Upserted={ProductsUpserted}, Skipped={ProductsSkipped}. Message={Message}",
                result.ProductsUpserted,
                result.ProductsSkipped,
                result.Message
            );
        }
        else
        {
            _logger.LogWarning("Product sync did not complete successfully. Message={Message}", result.Message);
        }
    }
}
