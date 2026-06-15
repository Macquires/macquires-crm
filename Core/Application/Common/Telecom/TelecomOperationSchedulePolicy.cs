using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom;

/// <summary>
/// Resolves effective dates for BSS operations that can be deferred until a future UTC moment.
/// </summary>
public static class TelecomOperationSchedulePolicy
{
    public static readonly TimeSpan ImmediateExecutionGrace = TimeSpan.FromMinutes(1);

    public static bool SupportsScheduling(TelecomOperationKind kind) =>
        kind is TelecomOperationKind.Migration
            or TelecomOperationKind.ChangeGsmType
            or TelecomOperationKind.TakeOver
            or TelecomOperationKind.Termination
            or TelecomOperationKind.TemporarySuspension
            or TelecomOperationKind.NumberPortability
            or TelecomOperationKind.SimSwap
            or TelecomOperationKind.NewActivation
            or TelecomOperationKind.Reconnect
            or TelecomOperationKind.DepositRefundSettlement
            or TelecomOperationKind.BadDebtRecovery
            or TelecomOperationKind.DeviceSale;

    public static DateTime? ResolveEffectiveDateUtc(TelecomOperationRequest operation) =>
        operation.Kind switch
        {
            TelecomOperationKind.Migration => operation.MigrationEffectiveDateUtc,
            TelecomOperationKind.ChangeGsmType => operation.GsmEffectiveDateUtc,
            TelecomOperationKind.TakeOver => operation.TakeOverEffectiveDateUtc,
            TelecomOperationKind.Termination => operation.TerminationEffectiveDateUtc,
            TelecomOperationKind.TemporarySuspension => operation.SuspensionStartDateUtc,
            TelecomOperationKind.NumberPortability => operation.NumberChangeEffectiveDateUtc,
            TelecomOperationKind.SimSwap => operation.SimSwapEffectiveDateUtc,
            TelecomOperationKind.NewActivation => operation.ActivationEffectiveDateUtc,
            TelecomOperationKind.Reconnect => operation.ReconnectEffectiveDateUtc,
            TelecomOperationKind.DepositRefundSettlement => operation.RefundEffectiveDateUtc,
            TelecomOperationKind.BadDebtRecovery => operation.BadDebtEffectiveDateUtc,
            TelecomOperationKind.DeviceSale => operation.DeviceSaleEffectiveDateUtc,
            _ => null,
        };

    public static bool ShouldDeferToScheduled(TelecomOperationRequest operation, DateTime utcNow)
    {
        if (!SupportsScheduling(operation.Kind))
        {
            return false;
        }

        return ResolveEffectiveDateUtc(operation) is { } effective
               && effective > utcNow.Add(ImmediateExecutionGrace);
    }

    public static bool IsDueForExecution(TelecomOperationRequest operation, DateTime utcNow) =>
        operation.Status == TelecomOperationStatus.Scheduled
        && ResolveEffectiveDateUtc(operation) is { } effective
        && effective <= utcNow.Add(ImmediateExecutionGrace);
}
