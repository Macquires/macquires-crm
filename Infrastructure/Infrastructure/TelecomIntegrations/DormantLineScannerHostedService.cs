using Application.Common.Telecom;
using Application.Common.Telecom.Inventory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>Case C — scans prepaid lines with no recharge/usage activity and force-recycles them into quarantine.</summary>
public sealed class DormantLineScannerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DormantLineScannerHostedService> _logger;

    public DormantLineScannerHostedService(
        IServiceProvider serviceProvider,
        ILogger<DormantLineScannerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DormantLineScannerHostedService starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromHours(6);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var rules = scope.ServiceProvider.GetRequiredService<ITelecomInventoryRulesProvider>();
                var hours = await rules.GetDormantLineScanIntervalHoursAsync(stoppingToken);
                if (hours > 0)
                {
                    delay = TimeSpan.FromHours(hours);
                }

                var recycling = scope.ServiceProvider.GetRequiredService<IMsisdnRecyclingService>();
                var recycled = await recycling.ScanAndRecycleDormantLinesAsync(stoppingToken);
                if (recycled > 0)
                {
                    _logger.LogInformation(
                        "Dormant line scanner recycled {Count} prepaid lines into quarantine.",
                        recycled);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dormant line scanner worker failed.");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}
