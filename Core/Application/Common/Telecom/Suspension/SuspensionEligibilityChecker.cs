using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Suspension;

public sealed class SuspensionEligibilityChecker : ISuspensionEligibilityChecker
{
    private static readonly TelecomOperationStatus[] BlockingStatuses =
    [
        TelecomOperationStatus.Draft,
        TelecomOperationStatus.PendingDocuments,
        TelecomOperationStatus.Confirmed,
        TelecomOperationStatus.Provisioning,
        TelecomOperationStatus.PendingExternal
    ];

    private readonly IQueryContext _query;
    private readonly IBillingSystemIntegration _billing;

    public SuspensionEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<SuspensionEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string suspensionType,
        string suspensionReason,
        string barringLevel,
        bool autoReconnectEnabled,
        DateTime? suspensionEndDateUtc,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        var type = (suspensionType ?? string.Empty).Trim();
        var reason = (suspensionReason ?? string.Empty).Trim();
        var barring = (barringLevel ?? string.Empty).Trim();

        if (!SuspensionWellKnown.IsKnownType(type))
        {
            throw new BusinessRuleViolationException(
                "نوع الحظر غير معروف (CustomerRequest, Billing, Fraud, Regulatory, Operational).");
        }

        if (!SuspensionWellKnown.IsKnownBarringLevel(barring))
        {
            throw new BusinessRuleViolationException("مستوى الحظر غير معروف (Full, InboundOnly, OutboundOnly).");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleViolationException("سبب الحظر مطلوب.");
        }

        if (autoReconnectEnabled && !suspensionEndDateUtc.HasValue)
        {
            throw new BusinessRuleViolationException("VAL-08-04: تاريخ انتهاء الحظر مطلوب عند تفعيل إعادة الاتصال التلقائية.");
        }

        var profileId = subscriberProfileId.Trim();
        var assetId = msisdnAssetId.Trim();

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        EnsureCustomerEligible(profile.Customer);

        if (profile.OperationalStatus == SubscriberOperationalStatus.Terminated)
        {
            return Deny("VAL-08-01: لا يمكن حظر خط منتهٍ.", profile.CustomerId, null, assetId, false, "AlreadyTerminated");
        }

        if (profile.OperationalStatus is SubscriberOperationalStatus.Suspended
            or SubscriberOperationalStatus.SuspendedInbound
            or SubscriberOperationalStatus.SuspendedOutbound)
        {
            return Deny("VAL-08-01: الخط موقوف مسبقاً.", profile.CustomerId, null, assetId, false, "AlreadySuspended");
        }

        if (profile.OperationalStatus != SubscriberOperationalStatus.Active)
        {
            return Deny(
                $"VAL-08-01: لا يمكن الحظر — حالة المشترك {profile.OperationalStatus}.",
                profile.CustomerId,
                null,
                assetId,
                false,
                "SubscriberNotActive");
        }

        var asset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        if (asset.PoolStatus != MsisdnPoolStatus.Active)
        {
            return Deny(
                $"VAL-08-01: لا يمكن الحظر — حالة الرقم {asset.PoolStatus}.",
                profile.CustomerId,
                asset.Msisdn,
                assetId,
                false,
                "LineNotActive");
        }

        if (!string.Equals(asset.SubscriberProfileId, profileId, StringComparison.Ordinal))
        {
            return Deny(
                "VAL-08-01: الرقم غير مربوط بملف المشترك.",
                profile.CustomerId,
                asset.Msisdn,
                assetId,
                false,
                "LineNotOwned");
        }

        if (string.Equals(type, SuspensionWellKnown.Billing, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(asset.Msisdn))
        {
            var balance = await _billing.GetOutstandingBalanceAsync(asset.Msisdn, cancellationToken);
            if (balance >= 0)
            {
                return Deny(
                    "VAL-08-01: حظر الفواتير يتطلب ذمماً مالية أو مرحلة تحصيل — الرصيد غير سالب.",
                    profile.CustomerId,
                    asset.Msisdn,
                    assetId,
                    false,
                    "BillingNotApplicable");
            }
        }

        await EnsureNoBlockingOperationAsync(assetId, excludeOperationId, cancellationToken);

        var requiresBo = RequiresBackOfficeApproval(profile.Customer, type);
        var longReview = IsLongSuspensionReview(suspensionEndDateUtc);

        return new SuspensionEligibilityResult(
            true,
            requiresBo
                ? "حظر الخط مسموح — بانتظار اعتماد الباك أوفيس."
                : "حظر الخط مسموح.",
            profile.CustomerId,
            asset.Msisdn,
            assetId,
            requiresBo,
            requiresBo ? "BackOfficePending" : "Allowed",
            longReview);
    }

    public async Task<SuspensionEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operation.SuspensionType))
        {
            throw new BusinessRuleViolationException("نوع الحظر غير مسجل في الطلب.");
        }

        var assetId = operation.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("رقم الخط غير محدد في الطلب.");

        return await ValidateForCreateAsync(
            operation.SubscriberProfileId,
            assetId,
            operation.SuspensionType,
            operation.SuspensionReason ?? string.Empty,
            operation.BarringLevel ?? SuspensionWellKnown.BarringFull,
            operation.AutoReconnectEnabled,
            operation.SuspensionEndDateUtc,
            operation.Id,
            cancellationToken);
    }

    private static bool RequiresBackOfficeApproval(Customer? customer, string suspensionType)
    {
        if (SuspensionWellKnown.IsBackOfficeType(suspensionType))
        {
            return true;
        }

        return customer?.CustomerKind == CustomerKind.Corporate;
    }

    private static bool IsLongSuspensionReview(DateTime? endUtc) =>
        endUtc.HasValue && endUtc.Value - DateTime.UtcNow > SuspensionWellKnown.LongSuspensionThreshold;

    private static void EnsureCustomerEligible(Customer? customer)
    {
        if (customer == null)
        {
            throw new BusinessRuleViolationException("العميل غير موجود.");
        }

        if (customer.Status == CustomerStatus.Blacklisted)
        {
            throw new BusinessRuleViolationException("VAL-08-02: العميل على القائمة السوداء.");
        }

        if (customer.Status == CustomerStatus.Closed)
        {
            throw new BusinessRuleViolationException("VAL-08-01: حساب العميل مغلق.");
        }
    }

    private async Task EnsureNoBlockingOperationAsync(
        string msisdnAssetId,
        string? excludeOperationId,
        CancellationToken cancellationToken)
    {
        var query = _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.MsisdnAssetId == msisdnAssetId && BlockingStatuses.Contains(o.Status));

        if (!string.IsNullOrEmpty(excludeOperationId))
        {
            query = query.Where(o => o.Id != excludeOperationId);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessRuleViolationException(
                "VAL-08-01: يوجد عملية تليكوم مفتوحة على هذا الخط — أغلقها أو انتظر اكتمالها.");
        }
    }

    private static SuspensionEligibilityResult Deny(
        string messageAr,
        string? customerId,
        string? msisdn,
        string? msisdnAssetId,
        bool requiresBo,
        string code) =>
        new(false, messageAr, customerId, msisdn, msisdnAssetId, requiresBo, code, false);
}
