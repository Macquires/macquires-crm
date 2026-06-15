using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Settings;
using Infrastructure.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations.Oracle;

public sealed class OracleInventorySyncHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OracleInventorySyncHostedService> _logger;

    public OracleInventorySyncHostedService(
        IServiceProvider serviceProvider,
        ILogger<OracleInventorySyncHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OracleInventorySyncHostedService starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = 60;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                using (scope.ServiceProvider.GetRequiredService<ISystemExecutionGate>().Enter())
                {
                    var settings = scope.ServiceProvider.GetRequiredService<IGlobalSettingsProvider>();
                    var enabled = await settings.GetBoolAsync(GlobalSettingKeys.IntegrationOracleFusionEnabled, defaultValue: true, stoppingToken);
                    intervalMinutes = await settings.GetIntAsync(
                        GlobalSettingKeys.IntegrationOracleFusionSyncIntervalMinutes,
                        defaultValue: 60,
                        min: 5,
                        max: 1440,
                        cancellationToken: stoppingToken);

                    if (enabled)
                    {
                        await scope.TryRunUnderDistributedLockAsync(
                            "hosted-oracle-inventory-sync",
                            TimeSpan.FromMinutes(Math.Max(intervalMinutes - 1, 5)),
                            async ct =>
                            {
                                var sync = scope.ServiceProvider.GetRequiredService<IOracleInventorySyncService>();
                                var result = await sync.SyncAsync(ct);
                                _logger.LogInformation("Oracle inventory sync: {Message}", result.Message);
                            },
                            stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oracle inventory sync failed.");
                intervalMinutes = Math.Min(intervalMinutes, 15);
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
