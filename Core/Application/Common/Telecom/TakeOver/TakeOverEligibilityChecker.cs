using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.TakeOver;

public sealed class TakeOverEligibilityChecker : ITakeOverEligibilityChecker
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

    public TakeOverEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<TakeOverEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string secondarySubscriberProfileId,
        string? msisdnAssetId,
        string transferReason,
        string? excludeOperationId = null,
        string? obligationSettlementReference = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transferReason))
        {
            throw new BusinessRuleViolationException("سبب نقل الملكية مطلوب.");
        }

        var oldProfileId = subscriberProfileId.Trim();
        var newProfileId = secondarySubscriberProfileId.Trim();

        if (string.Equals(oldProfileId, newProfileId, StringComparison.Ordinal))
        {
            return Deny("لا يمكن نقل الملكية لنفس ملف المشترك.", null, null, null, "SameProfile");
        }

        var (oldProfile, newProfile) = await ResolveProfilesAsync(oldProfileId, newProfileId, cancellationToken);

        if (string.Equals(oldProfile.CustomerId, newProfile.CustomerId, StringComparison.Ordinal))
        {
            return Deny("المالك الجديد يجب أن يكون عميلاً مختلفاً عن المالك الحالي.", oldProfile.CustomerId, newProfile.CustomerId, null, "SameCustomer");
        }

        EnsureCustomerEligible(oldProfile.Customer, "المالك الحالي");
        EnsureCustomerEligible(newProfile.Customer, "المالك الجديد");

        var msisdnId = (msisdnAssetId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(msisdnId))
        {
            throw new BusinessRuleViolationException("يجب اختيار خط (MSISDN) لنقل الملكية.");
        }

        var asset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == msisdnId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        if (asset.PoolStatus != MsisdnPoolStatus.Active)
        {
            return Deny(
                $"لا يمكن نقل ملكية الرقم {asset.Msisdn} — الحالة {asset.PoolStatus} وليست Active.",
                oldProfile.CustomerId,
                newProfile.CustomerId,
                asset.Msisdn,
                "MsisdnNotActive");
        }

        if (!string.IsNullOrEmpty(asset.Msisdn))
        {
            await EnsureNoOutstandingDebtAsync(asset.Msisdn, obligationSettlementReference, cancellationToken);
        }

        await EnsureNoBlockingTakeOverAsync(msisdnId, excludeOperationId, cancellationToken);

        return new TakeOverEligibilityResult(
            true,
            "نقل الملكية مسموح — بانتظار رفع الهوية واعتماد الباك أوفيس.",
            oldProfile.CustomerId,
            newProfile.CustomerId,
            asset.Msisdn,
            "Allowed");
    }

    public void ValidateDocumentsForConfirm(TelecomOperationRequest operation)
    {
        if (operation.Kind != TelecomOperationKind.TakeOver)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(operation.IdentityDocumentStorageKey))
        {
            throw new BusinessRuleViolationException("VAL-07-01: وثيقة هوية المالك الجديد مطلوبة قبل الاعتماد.");
        }

        if (operation.DocumentStatus is not TelecomDocumentStatus.Uploaded and not TelecomDocumentStatus.Verified)
        {
            throw new BusinessRuleViolationException("VAL-07-01: حالة الوثيقة لا تسمح بالاعتماد.");
        }
    }

    public async Task<TakeOverEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        ValidateDocumentsForConfirm(operation);

        if (string.IsNullOrWhiteSpace(operation.TransferReason))
        {
            throw new BusinessRuleViolationException("سبب نقل الملكية غير مسجل في الطلب.");
        }

        return await ValidateForCreateAsync(
            operation.SubscriberProfileId,
            operation.SecondarySubscriberProfileId!,
            operation.MsisdnAssetId,
            operation.TransferReason,
            operation.Id,
            string.Equals(operation.TakeOverObligationStatus, "Settled", StringComparison.OrdinalIgnoreCase)
                ? operation.PaymentReference
                : null,
            cancellationToken);
    }

    private static void EnsureCustomerEligible(Customer? customer, string partyLabelAr)
    {
        if (customer == null)
        {
            throw new BusinessRuleViolationException($"{partyLabelAr}: العميل غير موجود.");
        }

        if (customer.Status == CustomerStatus.Blacklisted)
        {
            throw new BusinessRuleViolationException($"VAL-07-05: {partyLabelAr} على القائمة السوداء.");
        }

        if (customer.Status == CustomerStatus.Closed)
        {
            throw new BusinessRuleViolationException($"{partyLabelAr}: حساب العميل مغلق.");
        }
    }

    private async Task EnsureNoOutstandingDebtAsync(
        string msisdn,
        string? obligationSettlementReference,
        CancellationToken cancellationToken)
    {
        var balance = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
        if (balance < 0)
        {
            if (!string.IsNullOrWhiteSpace(obligationSettlementReference))
            {
                return;
            }

            throw new BusinessRuleViolationException(
                $"VAL-07-02: لا يمكن نقل الملكية — ذمم مالية معلقة بقيمة {-balance:N0} ل.س على الخط {msisdn}.");
        }
    }

    private async Task EnsureNoBlockingTakeOverAsync(
        string msisdnAssetId,
        string? excludeOperationId,
        CancellationToken cancellationToken)
    {
        var query = _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.Kind == TelecomOperationKind.TakeOver
                        && o.MsisdnAssetId == msisdnAssetId
                        && BlockingStatuses.Contains(o.Status));

        if (!string.IsNullOrEmpty(excludeOperationId))
        {
            query = query.Where(o => o.Id != excludeOperationId);
        }

        var hasBlocking = await query.AnyAsync(cancellationToken);

        if (hasBlocking)
        {
            throw new BusinessRuleViolationException(
                "VAL-07-03: يوجد طلب نقل ملكية مفتوح على نفس الخط.");
        }
    }

    private async Task<(SubscriberProfile Old, SubscriberProfile New)> ResolveProfilesAsync(
        string oldProfileId,
        string newProfileId,
        CancellationToken cancellationToken)
    {
        var profiles = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.Id == oldProfileId || p.Id == newProfileId)
            .Include(p => p.Customer)
            .ToListAsync(cancellationToken);

        var oldProfile = profiles.FirstOrDefault(p => p.Id == oldProfileId)
            ?? throw new BusinessRuleViolationException("ملف المالك الحالي غير موجود.");
        var newProfile = profiles.FirstOrDefault(p => p.Id == newProfileId)
            ?? throw new BusinessRuleViolationException("ملف المالك الجديد غير موجود.");

        return (oldProfile, newProfile);
    }

    private static TakeOverEligibilityResult Deny(
        string messageAr,
        string? oldCustomerId,
        string? newCustomerId,
        string? msisdn,
        string code) =>
        new(false, messageAr, oldCustomerId, newCustomerId, msisdn, code);
}
