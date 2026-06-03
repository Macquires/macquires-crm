using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Telecom.Suspension;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Reconnect;

public sealed class ReconnectEligibilityChecker : IReconnectEligibilityChecker
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

    public ReconnectEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<ReconnectEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string reconnectReason,
        string clearanceType,
        string? paymentReference,
        bool fraudClearanceConfirmed,
        string? sourceSuspensionOperationId,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        var reason = (reconnectReason ?? string.Empty).Trim();
        var clearance = (clearanceType ?? string.Empty).Trim();

        if (!ReconnectWellKnown.IsKnownClearanceType(clearance))
        {
            throw new BusinessRuleViolationException(
                "نوع التسوية غير معروف (Payment, Fraud, Regulatory, Customer, Operational).");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleViolationException("سبب إعادة التفعيل مطلوب.");
        }

        var profileId = subscriberProfileId.Trim();
        var assetId = msisdnAssetId.Trim();

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        if (profile.OperationalStatus == SubscriberOperationalStatus.Terminated)
        {
            return Deny(
                "VAL-09-05: الخط ملغي نهائياً — يرجى استخدام مسار تفعيل خط جديد.",
                profile.CustomerId,
                null,
                assetId,
                null,
                false,
                "RequiresNewActivation");
        }

        if (profile.Customer?.Status == CustomerStatus.Blacklisted)
        {
            return Deny(
                "VAL-09-01: إعادة التفعيل مرفوضة — العميل على القائمة السوداء.",
                profile.CustomerId,
                null,
                assetId,
                null,
                false,
                "Blacklisted");
        }

        if (profile.OperationalStatus == SubscriberOperationalStatus.Active)
        {
            return Deny("VAL-09-01: الخط نشط مسبقاً.", profile.CustomerId, null, assetId, null, false, "AlreadyActive");
        }

        if (profile.OperationalStatus is not (
            SubscriberOperationalStatus.Suspended
            or SubscriberOperationalStatus.SuspendedInbound
            or SubscriberOperationalStatus.SuspendedOutbound))
        {
            return Deny(
                $"VAL-09-01: لا يمكن إعادة التفعيل — حالة المشترك {profile.OperationalStatus}.",
                profile.CustomerId,
                null,
                assetId,
                null,
                false,
                "NotSuspended");
        }

        var asset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        if (asset.PoolStatus != MsisdnPoolStatus.Suspended)
        {
            return Deny(
                $"VAL-09-01: حالة الرقم {asset.PoolStatus} — يتوقع Suspended.",
                profile.CustomerId,
                asset.Msisdn,
                assetId,
                null,
                false,
                "MsisdnNotSuspended");
        }

        var sourceOpId = await ResolveSourceSuspensionOperationIdAsync(
            assetId,
            sourceSuspensionOperationId,
            cancellationToken);

        var lastSuspensionType = sourceOpId == null
            ? null
            : await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
                .Where(o => o.Id == sourceOpId)
                .Select(o => o.SuspensionType)
                .FirstOrDefaultAsync(cancellationToken);

        if (string.Equals(lastSuspensionType, SuspensionWellKnown.Billing, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clearance, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(paymentReference))
            {
                return Deny(
                    "VAL-09-02: مرجع الدفع مطلوب لتسوية حظر الفواتير.",
                    profile.CustomerId,
                    asset.Msisdn,
                    assetId,
                    sourceOpId,
                    false,
                    "PaymentReferenceRequired");
            }

            if (!string.IsNullOrEmpty(asset.Msisdn))
            {
                var balance = await _billing.GetOutstandingBalanceAsync(asset.Msisdn, cancellationToken);
                if (balance < 0)
                {
                    return Deny(
                        $"VAL-09-02: ذمم مالية بقيمة {-balance:N0} ل.س — يجب التسوية قبل إعادة التفعيل.",
                        profile.CustomerId,
                        asset.Msisdn,
                        assetId,
                        sourceOpId,
                        false,
                        "OutstandingDebt");
                }
            }
        }

        var requiresBo = string.Equals(lastSuspensionType, SuspensionWellKnown.Fraud, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clearance, ReconnectWellKnown.Fraud, StringComparison.OrdinalIgnoreCase)
            || string.Equals(lastSuspensionType, SuspensionWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase);

        if (requiresBo && !fraudClearanceConfirmed)
        {
            return Deny(
                "VAL-09-03: إزالة حظر الاحتيال/التنظيمي تتطلب اعتماد الباك أوفيس وتأكيد التسوية.",
                profile.CustomerId,
                asset.Msisdn,
                assetId,
                sourceOpId,
                true,
                "FraudClearanceRequired");
        }

        await EnsureNoBlockingOperationAsync(assetId, excludeOperationId, cancellationToken);

        return new ReconnectEligibilityResult(
            true,
            requiresBo
                ? "إعادة التفعيل مسموحة — بانتظار اعتماد الباك أوفيس."
                : "إعادة التفعيل مسموحة.",
            profile.CustomerId,
            asset.Msisdn,
            assetId,
            sourceOpId,
            requiresBo,
            requiresBo ? "BackOfficePending" : "Allowed");
    }

    public async Task<ReconnectEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        var assetId = operation.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("رقم الخط غير محدد في الطلب.");

        return await ValidateForCreateAsync(
            operation.SubscriberProfileId,
            assetId,
            operation.ReconnectReason ?? string.Empty,
            operation.ClearanceType ?? ReconnectWellKnown.Customer,
            operation.PaymentReference,
            operation.FraudClearanceConfirmed,
            operation.SourceSuspensionOperationId,
            operation.Id,
            cancellationToken);
    }

    private async Task<string?> ResolveSourceSuspensionOperationIdAsync(
        string msisdnAssetId,
        string? explicitId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(explicitId))
        {
            return explicitId.Trim();
        }

        return await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.Kind == TelecomOperationKind.TemporarySuspension
                        && o.MsisdnAssetId == msisdnAssetId
                        && o.Status == TelecomOperationStatus.Completed)
            .OrderByDescending(o => o.ConfirmedAtUtc ?? o.CreatedAtUtc)
            .Select(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);
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
                "VAL-09-01: يوجد عملية تليكوم مفتوحة على هذا الخط.");
        }
    }

    private static ReconnectEligibilityResult Deny(
        string messageAr,
        string? customerId,
        string? msisdn,
        string? msisdnAssetId,
        string? sourceSuspensionOperationId,
        bool requiresBo,
        string code) =>
        new(false, messageAr, customerId, msisdn, msisdnAssetId, sourceSuspensionOperationId, requiresBo, code);
}
