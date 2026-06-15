using Application.Common.Integrations;
using Application.Common.Telecom.Billing;
using Application.Common.Telecom.HlrFailure;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom;

public sealed class TelecomHlrFailureCompensator : ITelecomHlrFailureCompensator
{
    private const string CompensationMessageAr =
        "فشل تزويد الشبكة (HLR) — تم عكس الحساب المالي (CBS) وإلغاء الربط المحلي.";

    private readonly IBillingRoutingOrchestrator _billingRouting;
    private readonly IHlrFailureLocalRevertService _localRevert;

    public TelecomHlrFailureCompensator(
        IBillingRoutingOrchestrator billingRouting,
        IHlrFailureLocalRevertService localRevert)
    {
        _billingRouting = billingRouting;
        _localRevert = localRevert;
    }

    public async Task<HlrFailureCompensationResult> CompensateAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        string? actorUserId,
        string hlrErrorMessage,
        CancellationToken cancellationToken)
    {
        var billingRequest = TelecomProvisionRequestBuilder.ToBillingRequest(
            operation,
            lineContext,
            TelecomBillingProvisionPhase.Reverse);

        var routing = _billingRouting.ResolveProvisionRouting(
            operation.Kind,
            lineContext.SubscriptionTypeCode,
            lineContext.SourceSubscriptionTypeCode,
            lineContext.TargetSubscriptionTypeCode);

        var compensation = await _billingRouting.CompensateProvisionAsync(
            operation,
            lineContext,
            billingRequest,
            cancellationToken);

        var cbsReversed = routing.UsesCbs && compensation.Success;
        var inReversed = routing.UsesIn && compensation.Success;
        var localCompensated = await _localRevert.RevertLocalBindAsync(operation, actorUserId, cancellationToken);

        var msg = operation.Kind switch
        {
            TelecomOperationKind.TakeOver =>
                $"VAL-07-04: فشل HLR — تم عكس CBS واستعادة المالك السابق ({hlrErrorMessage})",
            TelecomOperationKind.SimSwap =>
                $"VAL-04-ROLLBACK: فشل HLR — تم عكس CBS واستعادة الشريحة السابقة ({hlrErrorMessage})",
            TelecomOperationKind.NumberPortability =>
                $"VAL-05-ROLLBACK: فشل HLR — تم عكس CBS واستعادة الرقم السابق ({hlrErrorMessage})",
            TelecomOperationKind.Termination =>
                $"VAL-10-ROLLBACK: فشل HLR — تم عكس CBS واستعادة الخط ({hlrErrorMessage})",
            TelecomOperationKind.Migration =>
                $"VAL-11-ROLLBACK: فشل HLR — تم عكس CBS واستعادة الباقة السابقة ({hlrErrorMessage})",
            TelecomOperationKind.TemporarySuspension =>
                $"VAL-08-ROLLBACK: فشل HLR — تم عكس CBS واستعادة حالة الخط ({hlrErrorMessage})",
            TelecomOperationKind.Reconnect =>
                $"VAL-09-ROLLBACK: فشل HLR — تم عكس CBS وإعادة الحظر ({hlrErrorMessage})",
            _ when routing.UsesIn && !routing.UsesCbs =>
                $"فشل HLR — تم عكس IN (Quarantined) وإلغاء الربط المحلي ({hlrErrorMessage})",
            _ => $"{CompensationMessageAr} ({hlrErrorMessage})",
        };

        return new HlrFailureCompensationResult(cbsReversed, inReversed, localCompensated, msg);
    }
}
