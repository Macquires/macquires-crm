using Application.Common;
using Application.Common.Integrations;
using Application.Common.Telecom.OperationConfirm;
using Domain.Entities;

namespace Application.Common.Telecom;

internal static class TelecomActivationWorkflowSupport
{
    internal const string IdempotentMessageAr = "الطلب معالج مسبقاً.";

    internal static TelecomActivationWorkflowResult Fail(TelecomOperationRequest entity, string message) =>
        Fail(entity, new BilingualUserMessage(message, message));

    internal static TelecomActivationWorkflowResult Fail(TelecomOperationRequest entity, BilingualUserMessage message) =>
        new(
            entity,
            new BillingProvisionResult(false, message.ResolveForCurrentCulture()),
            null,
            false,
            message.ResolveForCurrentCulture(),
            message.Ar,
            message.En);

    internal static string BuildScheduledMessageAr(TelecomOperationRequest entity)
    {
        var effective = TelecomOperationSchedulePolicy.ResolveEffectiveDateUtc(entity);
        return effective.HasValue
            ? $"تم جدولة العملية للتنفيذ في {effective.Value:yyyy-MM-dd HH:mm} UTC."
            : "تم جدولة العملية للتنفيذ لاحقاً.";
    }

    internal static string BuildScheduledMessageEn(TelecomOperationRequest entity)
    {
        var effective = TelecomOperationSchedulePolicy.ResolveEffectiveDateUtc(entity);
        return effective.HasValue
            ? $"Operation scheduled for {effective.Value:yyyy-MM-dd HH:mm} UTC."
            : "Operation scheduled for later execution.";
    }

    internal static bool HasProvisionPayload(OperationApplyResult result) =>
        !string.IsNullOrEmpty(result.Msisdn)
        || !string.IsNullOrEmpty(result.TelecomSubscriptionId)
        || !string.IsNullOrEmpty(result.CustomerId);

    internal static OperationProvisionContext ToProvisionContext(
        OperationApplyResult result,
        TelecomOperationRequest entity) =>
        new(
            result.Msisdn,
            result.Iccid,
            result.CustomerId,
            result.SubscriberProfileId,
            result.TelecomSubscriptionId,
            entity.MsisdnAssetId ?? entity.TargetMsisdnAssetId,
            entity.SimInventoryId);
}
