using Application.Common.Integrations;
using Application.Common.Settings;
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
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var settings = scope.ServiceProvider.GetRequiredService<IGlobalSettingsProvider>();
                var enabled = await settings.GetBoolAsync(GlobalSettingKeys.IntegrationOracleFusionEnabled, defaultValue: true, stoppingToken);
                var intervalMinutes = await settings.GetIntAsync(
                    GlobalSettingKeys.IntegrationOracleFusionSyncIntervalMinutes,
                    defaultValue: 60,
                    min: 5,
                    max: 1440,
                    cancellationToken: stoppingToken);

                if (enabled)
                {
                    var sync = scope.ServiceProvider.GetRequiredService<IOracleInventorySyncService>();
                    var result = await sync.SyncAsync(stoppingToken);
                    _logger.LogInformation("Oracle inventory sync: {Message}", result.Message);
                }

                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oracle inventory sync failed.");
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
