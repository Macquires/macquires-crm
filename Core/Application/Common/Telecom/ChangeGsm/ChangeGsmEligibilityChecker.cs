using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.ChangeGsm;

public sealed class ChangeGsmEligibilityChecker : IChangeGsmEligibilityChecker
{
    private readonly IQueryContext _query;
    private readonly IBillingSystemIntegration _billing;

    public ChangeGsmEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<ChangeGsmEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string? msisdnAssetId,
        string targetSubscriptionTypeId,
        string? migrationReason,
        CancellationToken cancellationToken = default)
    {
        var targetId = targetSubscriptionTypeId.Trim();
        if (string.IsNullOrEmpty(targetId))
        {
            throw new BusinessRuleViolationException("نوع الخط الهدف مطلوب.");
        }

        if (string.IsNullOrWhiteSpace(migrationReason))
        {
            throw new BusinessRuleViolationException("سبب التحويل مطلوب.");
        }

        var (subscription, asset) = await ResolveSubscriptionAsync(
            subscriberProfileId,
            msisdnAssetId,
            cancellationToken);

        var sourceId = subscription.SubscriptionTypeId;
        if (string.Equals(sourceId, targetId, StringComparison.Ordinal))
        {
            return new ChangeGsmEligibilityResult(
                false,
                "نوع الخط الهدف مطابق للنوع الحالي.",
                sourceId,
                subscription.SubscriptionTypeLookup?.Code,
                await ResolveTypeCodeAsync(targetId, cancellationToken),
                "SameType");
        }

        if (!ChangeGsmTransitionMatrix.IsAllowed(sourceId, targetId))
        {
            return new ChangeGsmEligibilityResult(
                false,
                "VAL-06-01: هذا الانتقال بين أنواع الخط غير مسموح.",
                sourceId,
                subscription.SubscriptionTypeLookup?.Code,
                await ResolveTypeCodeAsync(targetId, cancellationToken),
                "TransitionBlocked");
        }

        EnsureMsisdnActive(asset, asset?.Msisdn);

        var msisdn = asset?.Msisdn ?? await ResolveMsisdnAsync(subscription, cancellationToken);
        if (!string.IsNullOrEmpty(msisdn))
        {
            await EnsureNoOutstandingDebtAsync(sourceId, targetId, msisdn, cancellationToken);
        }

        var targetCode = await ResolveTypeCodeAsync(targetId, cancellationToken);

        return new ChangeGsmEligibilityResult(
            true,
            $"الانتقال مسموح من {subscription.SubscriptionTypeLookup?.NameAr ?? sourceId} إلى النوع الجديد.",
            sourceId,
            subscription.SubscriptionTypeLookup?.Code,
            targetCode,
            "Allowed");
    }

    public void EnsureMsisdnActive(MsisdnAsset? asset, string? msisdn)
    {
        if (asset == null)
        {
            throw new BusinessRuleViolationException("لا يوجد رقم خط مرتبط بالعملية.");
        }

        if (asset.PoolStatus != MsisdnPoolStatus.Active)
        {
            throw new BusinessRuleViolationException(
                $"لا يمكن تغيير نوع الخط — حالة الرقم {msisdn ?? asset.Msisdn} هي {asset.PoolStatus} وليست Active.");
        }
    }

    private async Task EnsureNoOutstandingDebtAsync(
        string sourceTypeId,
        string targetTypeId,
        string msisdn,
        CancellationToken cancellationToken)
    {
        var balance = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);

        var leavingPostpaid = string.Equals(
            sourceTypeId,
            TelecomSubscriptionTypeWellKnownIds.Postpaid,
            StringComparison.Ordinal);

        if (leavingPostpaid && balance < 0)
        {
            throw new BusinessRuleViolationException(
                $"VAL-06-03: لا يمكن التحويل — ذمم مالية معلقة بقيمة {-balance:N0} ل.س على الخط {msisdn}.");
        }

        var upgradingToPostpaid = string.Equals(
            targetTypeId,
            TelecomSubscriptionTypeWellKnownIds.Postpaid,
            StringComparison.Ordinal);

        if (upgradingToPostpaid && balance < 0)
        {
            throw new BusinessRuleViolationException(
                $"VAL-06-03: لا يمكن الترقية إلى فاتورة — يوجد رصيد/ذمم سالبة على الخط {msisdn}.");
        }
    }

    private async Task<(TelecomSubscription Subscription, MsisdnAsset? Asset)> ResolveSubscriptionAsync(
        string subscriberProfileId,
        string? msisdnAssetId,
        CancellationToken cancellationToken)
    {
        var subs = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfileId == subscriberProfileId)
            .Include(s => s.SubscriptionTypeLookup)
            .Include(s => s.MsisdnAsset)
            .ToListAsync(cancellationToken);

        if (subs.Count == 0)
        {
            throw new BusinessRuleViolationException("لا يوجد اشتراك مرتبط بملف المشترك.");
        }

        var msisdnId = (msisdnAssetId ?? string.Empty).Trim();
        TelecomSubscription? chosen = null;
        if (!string.IsNullOrEmpty(msisdnId))
        {
            chosen = subs.FirstOrDefault(s => s.MsisdnAssetId == msisdnId);
        }

        chosen ??= subs.FirstOrDefault(s => s.IsPrimaryLine) ?? subs[0];
        return (chosen, chosen.MsisdnAsset);
    }

    private async Task<string?> ResolveMsisdnAsync(
        TelecomSubscription subscription,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(subscription.MsisdnAssetId))
        {
            return await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
                .Where(m => m.Id == subscription.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private async Task<string?> ResolveTypeCodeAsync(string typeId, CancellationToken cancellationToken) =>
        await _query.TelecomSubscriptionTypeLookup.AsNoTracking().IsDeletedEqualTo()
            .Where(t => t.Id == typeId && t.IsActive)
            .Select(t => t.Code)
            .FirstOrDefaultAsync(cancellationToken);
}
