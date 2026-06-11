using Application.Common.Telecom.Inventory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>Case C — releases MSISDN/SIM from quarantine after cooling period expires.</summary>
public sealed class MsisdnQuarantineRecyclingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MsisdnQuarantineRecyclingHostedService> _logger;

    public MsisdnQuarantineRecyclingHostedService(
        IServiceProvider serviceProvider,
        ILogger<MsisdnQuarantineRecyclingHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MsisdnQuarantineRecyclingHostedService starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var recycling = scope.ServiceProvider.GetRequiredService<IMsisdnRecyclingService>();
                var released = await recycling.ReleaseExpiredQuarantineAsync(stoppingToken);
                if (released > 0)
                {
                    _logger.LogInformation("Released {Count} quarantined inventory records back to Available.", released);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quarantine recycling worker failed.");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
