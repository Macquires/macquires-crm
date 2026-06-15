using Application.Common.Security;
using Application.Common.Telecom.Inventory;
using Infrastructure.Distributed;
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
                using (scope.ServiceProvider.GetRequiredService<ISystemExecutionGate>().Enter())
                {
                    await scope.TryRunUnderDistributedLockAsync(
                        "hosted-msisdn-quarantine-recycling",
                        TimeSpan.FromMinutes(30),
                        async ct =>
                        {
                var recycling = scope.ServiceProvider.GetRequiredService<IMsisdnRecyclingService>();
                var released = await recycling.ReleaseExpiredQuarantineAsync(ct);
                if (released > 0)
                {
                    _logger.LogInformation("Released {Count} quarantined inventory records back to Available.", released);
                }
                        },
                        stoppingToken);
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
