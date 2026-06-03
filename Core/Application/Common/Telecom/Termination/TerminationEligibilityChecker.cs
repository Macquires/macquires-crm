using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Termination;

public sealed class TerminationEligibilityChecker : ITerminationEligibilityChecker
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

    public TerminationEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<TerminationEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string terminationType,
        string terminationReason,
        string? retentionOfferOutcome,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        var type = (terminationType ?? string.Empty).Trim();
        var reason = (terminationReason ?? string.Empty).Trim();

        if (!TerminationWellKnown.IsKnownType(type))
        {
            throw new BusinessRuleViolationException("نوع الإنهاء غير معروف (Voluntary, Collections, Regulatory, Fraud).");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleViolationException("سبب الإنهاء مطلوب.");
        }

        if (string.Equals(type, TerminationWellKnown.Voluntary, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(retentionOfferOutcome))
        {
            throw new BusinessRuleViolationException(
                "VAL-10-05: يجب تسجيل نتيجة عرض الاحتفاظ قبل إنهاء الخط طوعياً.");
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
            return Deny(
                "VAL-10-04: ملف المشترك منتهٍ مسبقاً.",
                profile.CustomerId,
                null,
                assetId,
                null,
                false,
                "AlreadyTerminated");
        }

        var asset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        if (asset.PoolStatus != MsisdnPoolStatus.Active)
        {
            return Deny(
                $"VAL-10-04: لا يمكن الإنهاء — حالة الرقم {asset.PoolStatus}.",
                profile.CustomerId,
                asset.Msisdn,
                assetId,
                null,
                false,
                "LineNotActive");
        }

        if (!string.Equals(asset.SubscriberProfileId, profileId, StringComparison.Ordinal))
        {
            return Deny(
                "VAL-10-04: الرقم غير مربوط بملف المشترك.",
                profile.CustomerId,
                asset.Msisdn,
                assetId,
                null,
                false,
                "LineNotOwned");
        }

        if (!string.IsNullOrEmpty(asset.Msisdn))
        {
            await EnsureNoOutstandingDebtAsync(asset.Msisdn, cancellationToken);
        }

        await EnsureNoBlockingTerminationAsync(assetId, excludeOperationId, cancellationToken);

        var priorSimId = await ResolveActiveSimInventoryIdAsync(profileId, cancellationToken);

        var requiresBo = RequiresBackOfficeApproval(profile.Customer, type);

        return new TerminationEligibilityResult(
            true,
            requiresBo
                ? "إنهاء الخط مسموح — بانتظار اعتماد الباك أوفيس."
                : "إنهاء الخط مسموح.",
            profile.CustomerId,
            asset.Msisdn,
            assetId,
            priorSimId,
            requiresBo,
            requiresBo ? "BackOfficePending" : "Allowed");
    }

    public async Task<TerminationEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operation.TerminationType))
        {
            throw new BusinessRuleViolationException("نوع الإنهاء غير مسجل في الطلب.");
        }

        if (string.IsNullOrWhiteSpace(operation.TerminationReason))
        {
            throw new BusinessRuleViolationException("سبب الإنهاء غير مسجل في الطلب.");
        }

        var assetId = operation.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("رقم الخط غير محدد في الطلب.");

        var result = await ValidateForCreateAsync(
            operation.SubscriberProfileId,
            assetId,
            operation.TerminationType,
            operation.TerminationReason,
            operation.RetentionOfferOutcome,
            operation.Id,
            cancellationToken);

        if (!result.Allowed)
        {
            return result;
        }

        if (result.RequiresBackOfficeApproval
            && string.Equals(operation.TerminationType, TerminationWellKnown.Voluntary, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(operation.RetentionOfferOutcome))
        {
            return Deny(
                "VAL-10-05: نتيجة عرض الاحتفاظ مطلوبة.",
                result.CustomerId,
                result.Msisdn,
                result.MsisdnAssetId,
                result.PriorSimInventoryId,
                true,
                "RetentionMissing");
        }

        return result;
    }

    private static bool RequiresBackOfficeApproval(Customer? customer, string terminationType)
    {
        if (TerminationWellKnown.IsBackOfficeType(terminationType))
        {
            return true;
        }

        if (customer?.CustomerKind == CustomerKind.Corporate)
        {
            return true;
        }

        return false;
    }

    private static void EnsureCustomerEligible(Customer? customer)
    {
        if (customer == null)
        {
            throw new BusinessRuleViolationException("العميل غير موجود.");
        }

        if (customer.Status == CustomerStatus.Blacklisted)
        {
            throw new BusinessRuleViolationException("VAL-10-04: العميل على القائمة السوداء — لا يمكن إنهاء الخط.");
        }

        if (customer.Status == CustomerStatus.Closed)
        {
            throw new BusinessRuleViolationException("VAL-10-04: حساب العميل مغلق.");
        }
    }

    private async Task EnsureNoOutstandingDebtAsync(string msisdn, CancellationToken cancellationToken)
    {
        var balance = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
        if (balance < 0)
        {
            throw new BusinessRuleViolationException(
                $"VAL-10-02: ذمم مالية معلقة بقيمة {-balance:N0} ل.س — يجب التسوية قبل الإنهاء.");
        }
    }

    private async Task EnsureNoBlockingTerminationAsync(
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
                "VAL-10-01: يوجد عملية تليكوم مفتوحة على هذا الخط — أغلقها أو انتظر اكتمالها.");
        }
    }

    private async Task<string?> ResolveActiveSimInventoryIdAsync(
        string subscriberProfileId,
        CancellationToken cancellationToken)
    {
        var sim = await _query.SimInventory.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfileId == subscriberProfileId && s.Status == SimStatus.Active)
            .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return sim?.Id;
    }

    private static TerminationEligibilityResult Deny(
        string messageAr,
        string? customerId,
        string? msisdn,
        string? msisdnAssetId,
        string? priorSimInventoryId,
        bool requiresBo,
        string code) =>
        new(false, messageAr, customerId, msisdn, msisdnAssetId, priorSimInventoryId, requiresBo, code);
}
