using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Enums;
using Infrastructure.Distributed;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>
/// Executes BSS operations in <see cref="TelecomOperationStatus.Scheduled"/> once their effective date is due.
/// </summary>
public sealed class ScheduledTelecomOperationHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledTelecomOperationHostedService> _logger;

    public ScheduledTelecomOperationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledTelecomOperationHostedService> logger)
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
                await ProcessDueScheduledOperationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scheduled telecom operation worker failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessDueScheduledOperationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        using (scope.ServiceProvider.GetRequiredService<ISystemExecutionGate>().Enter())
        {
            await scope.TryRunUnderDistributedLockAsync(
                "hosted-scheduled-telecom-operations",
                TimeSpan.FromMinutes(5),
                async ct =>
                {
                    var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
                    var workflow = scope.ServiceProvider.GetRequiredService<ITelecomActivationWorkflow>();
                    var utcNow = DateTime.UtcNow;
                    var horizon = utcNow.Add(TelecomOperationSchedulePolicy.ImmediateExecutionGrace);

                    var dueIds = await query.TelecomOperationRequest.AsNoTracking()
                        .Where(o => !o.IsDeleted && o.Status == TelecomOperationStatus.Scheduled)
                        .Where(o =>
                            (o.Kind == TelecomOperationKind.Migration
                             && o.MigrationEffectiveDateUtc != null
                             && o.MigrationEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.ChangeGsmType
                                && o.GsmEffectiveDateUtc != null
                                && o.GsmEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.TakeOver
                                && o.TakeOverEffectiveDateUtc != null
                                && o.TakeOverEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.Termination
                                && o.TerminationEffectiveDateUtc != null
                                && o.TerminationEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.TemporarySuspension
                                && o.SuspensionStartDateUtc != null
                                && o.SuspensionStartDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.NumberPortability
                                && o.NumberChangeEffectiveDateUtc != null
                                && o.NumberChangeEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.SimSwap
                                && o.SimSwapEffectiveDateUtc != null
                                && o.SimSwapEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.NewActivation
                                && o.ActivationEffectiveDateUtc != null
                                && o.ActivationEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.Reconnect
                                && o.ReconnectEffectiveDateUtc != null
                                && o.ReconnectEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.DepositRefundSettlement
                                && o.RefundEffectiveDateUtc != null
                                && o.RefundEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.BadDebtRecovery
                                && o.BadDebtEffectiveDateUtc != null
                                && o.BadDebtEffectiveDateUtc <= horizon)
                            || (o.Kind == TelecomOperationKind.DeviceSale
                                && o.DeviceSaleEffectiveDateUtc != null
                                && o.DeviceSaleEffectiveDateUtc <= horizon))
                        .OrderBy(o => o.MigrationEffectiveDateUtc ?? o.GsmEffectiveDateUtc ?? o.TakeOverEffectiveDateUtc
                                      ?? o.TerminationEffectiveDateUtc ?? o.SuspensionStartDateUtc
                                      ?? o.NumberChangeEffectiveDateUtc ?? o.SimSwapEffectiveDateUtc
                                      ?? o.ActivationEffectiveDateUtc ?? o.ReconnectEffectiveDateUtc
                                      ?? o.RefundEffectiveDateUtc ?? o.BadDebtEffectiveDateUtc
                                      ?? o.DeviceSaleEffectiveDateUtc)
                        .Select(o => o.Id)
                        .Take(25)
                        .ToListAsync(ct);

                    foreach (var operationId in dueIds)
                    {
                        try
                        {
                            var result = await workflow.ExecuteScheduledOperationAsync(
                                operationId,
                                SystemOperatorContext.SystemUserId,
                                ct);

                            if (result.Operation.Status == TelecomOperationStatus.Completed)
                            {
                                _logger.LogInformation(
                                    "Scheduled operation {OperationId} executed successfully.",
                                    operationId);
                            }
                            else if (result.Operation.Status == TelecomOperationStatus.Failed)
                            {
                                _logger.LogWarning(
                                    "Scheduled operation {OperationId} failed: {Message}",
                                    operationId,
                                    result.MessageAr ?? result.Message);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Scheduled operation {OperationId} execution error.", operationId);
                        }
                    }
                },
                cancellationToken);
        }
    }
}
