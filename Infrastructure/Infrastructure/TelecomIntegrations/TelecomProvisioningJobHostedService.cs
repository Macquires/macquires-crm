using Application.Common.Security;
using Application.Common.Telecom;
using Infrastructure.Distributed;
using Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>
/// Background execution engine: replays approved operations stuck in PendingExternal (CBS done, HLR retry).
/// </summary>
public sealed class TelecomProvisioningJobHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelecomProvisioningJobHostedService> _logger;

    public TelecomProvisioningJobHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<TelecomProvisioningJobHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FlushPendingProvisioningAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Telecom provisioning job failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task FlushPendingProvisioningAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var gate = scope.ServiceProvider.GetRequiredService<ISystemExecutionGate>();
        using (gate.Enter())
        {
            await scope.TryRunUnderDistributedLockAsync(
                "hosted-telecom-provisioning-flush",
                TimeSpan.FromMinutes(2),
                async ct =>
                {
            var sync = scope.ServiceProvider.GetRequiredService<IPendingExternalSyncService>();
            var result = await sync.FlushAsync(SystemOperatorContext.SystemUserId, ct);

            if (result.Processed > 0)
            {
                _logger.LogInformation(
                    "Telecom provisioning job processed {Processed}: ok={Succeeded}, pending={StillPending}, failed={Failed}",
                    result.Processed,
                    result.Succeeded,
                    result.StillPending,
                    result.Failed);
            }
                },
                cancellationToken);
        }
    }
}
