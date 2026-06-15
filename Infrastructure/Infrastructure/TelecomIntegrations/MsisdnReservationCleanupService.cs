using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Enums;
using Infrastructure.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>
/// S03: Background service to periodically release MSISDNs that have been reserved but not confirmed.
/// </summary>
public class MsisdnReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MsisdnReservationCleanupService> _logger;

    public MsisdnReservationCleanupService(IServiceProvider serviceProvider, ILogger<MsisdnReservationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MsisdnReservationCleanupService starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var pollInterval = TimeSpan.FromMinutes(5);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                using (scope.ServiceProvider.GetRequiredService<ISystemExecutionGate>().Enter())
                {
                    await scope.TryRunUnderDistributedLockAsync(
                        "hosted-msisdn-reservation-cleanup",
                        TimeSpan.FromMinutes(4),
                        async ct =>
                        {
                    var inventoryRules = scope.ServiceProvider.GetRequiredService<ITelecomInventoryRulesProvider>();
                    var reservationDuration = await inventoryRules.GetMsisdnReservationDurationAsync(stoppingToken);
                    var pollMinutes = Math.Clamp(reservationDuration.TotalMinutes / 3, 1, 15);
                    pollInterval = TimeSpan.FromMinutes(pollMinutes);

                    var dbContext = scope.ServiceProvider.GetRequiredService<DataAccessManager.EFCore.Contexts.DataContext>();

                    var utcNow = DateTime.UtcNow;

                    var expiredReservations = await dbContext.MsisdnAsset
                        .Where(m => !m.IsDeleted
                            && m.PoolStatus == MsisdnPoolStatus.Reserved
                            && m.ReservedUntilUtc != null
                            && m.ReservedUntilUtc < utcNow)
                        .ToListAsync(stoppingToken);

                    if (expiredReservations.Count > 0)
                    {
                        foreach (var asset in expiredReservations)
                        {
                            asset.ReleaseReservationIfExpired(utcNow);
                            _logger.LogInformation("Released expired MSISDN reservation for {Msisdn}", asset.Msisdn);
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                        },
                        stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing MsisdnReservationCleanupService.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }
}
