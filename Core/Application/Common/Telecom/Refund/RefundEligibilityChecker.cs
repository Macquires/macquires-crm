using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Refund;

public sealed class RefundEligibilityChecker : IRefundEligibilityChecker
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

    public RefundEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<RefundEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string refundType,
        string refundMethod,
        decimal refundAmount,
        string refundReason,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        var type = (refundType ?? string.Empty).Trim();
        var method = (refundMethod ?? string.Empty).Trim();
        var reason = (refundReason ?? string.Empty).Trim();

        if (!RefundWellKnown.IsKnownType(type))
        {
            throw new BusinessRuleViolationException(
                "نوع الاسترداد غير معروف (Deposit, WalletBalance, Overpayment, SyriatelCash).");
        }

        if (!RefundWellKnown.IsKnownMethod(method))
        {
            throw new BusinessRuleViolationException(
                "طريقة الاسترداد غير معروفة (Cash, BankTransfer, WalletCredit, CreditNote).");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleViolationException("سبب الاسترداد مطلوب.");
        }

        if (refundAmount <= 0)
        {
            throw new BusinessRuleViolationException("VAL-15-01: مبلغ الاسترداد يجب أن يكون أكبر من صفر.");
        }

        var profileId = subscriberProfileId.Trim();
        var assetId = msisdnAssetId.Trim();

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        EnsureCustomerEligible(profile.Customer);

        var asset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        if (!string.Equals(asset.SubscriberProfileId, profileId, StringComparison.Ordinal))
        {
            return Deny(
                "VAL-15-03: الرقم غير مربوط بملف المشترك.",
                profile.CustomerId,
                asset.Msisdn,
                assetId,
                0m,
                0m,
                false,
                false,
                "LineNotBound");
        }

        var (depositSnap, walletSnap) = await BuildSnapshotsAsync(profile, asset.Msisdn, cancellationToken);

        if (refundAmount > RefundWellKnown.MaxRefundableForType(type, depositSnap, walletSnap))
        {
            return Deny(
                "VAL-15-02: مبلغ الاسترداد يتجاوز الرصيد المتاح في اللقطة المالية.",
                profile.CustomerId,
                asset.Msisdn,
                assetId,
                depositSnap,
                walletSnap,
                refundAmount > RefundWellKnown.DualApprovalThresholdSyp,
                true,
                "AmountExceedsSnapshot");
        }

        await EnsureNoBlockingRefundAsync(assetId, excludeOperationId, cancellationToken);

        var requiresDual = refundAmount > RefundWellKnown.DualApprovalThresholdSyp
                           || string.Equals(type, RefundWellKnown.TypeSyriatelCash, StringComparison.OrdinalIgnoreCase);
        var requiresBo = RefundWellKnown.RequiresBackOfficeApproval(type, refundAmount, method);

        return new RefundEligibilityResult(
            true,
            requiresBo
                ? "طلب الاسترداد — بانتظار اعتماد الباك أوفيس."
                : "طلب الاسترداد جاهز للتنفيذ.",
            profile.CustomerId,
            asset.Msisdn,
            assetId,
            depositSnap,
            walletSnap,
            requiresDual,
            requiresBo,
            "Allowed");
    }

    public async Task<RefundEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.DepositRefundSettlement)
        {
            throw new BusinessRuleViolationException("العملية ليست طلب استرداد مالي.");
        }

        if (string.IsNullOrEmpty(operation.MsisdnAssetId)
            || operation.RefundAmount is null or <= 0
            || string.IsNullOrWhiteSpace(operation.RefundType)
            || string.IsNullOrWhiteSpace(operation.RefundMethod))
        {
            throw new BusinessRuleViolationException("بيانات الاسترداد غير مكتملة في الطلب.");
        }

        if (operation.RequiresDualApproval
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal)
            && operation.DocumentStatus is not TelecomDocumentStatus.Uploaded and not TelecomDocumentStatus.Verified)
        {
            throw new BusinessRuleViolationException("VAL-15-04: وثيقة الاعتماد المالي مطلوبة قبل التنفيذ.");
        }

        var maxAllowed = RefundWellKnown.MaxRefundableForType(
            operation.RefundType!,
            operation.DepositBalanceSnapshot ?? 0m,
            operation.WalletBalanceSnapshot ?? 0m);

        if (operation.RefundAmount > maxAllowed)
        {
            throw new BusinessRuleViolationException("VAL-15-02: مبلغ الاسترداد يتجاوز اللقطة المالية المثبتة على الطلب.");
        }

        return await ValidateForCreateAsync(
            operation.SubscriberProfileId,
            operation.MsisdnAssetId,
            operation.RefundType,
            operation.RefundMethod,
            operation.RefundAmount.Value,
            operation.RefundReason ?? "—",
            operation.Id,
            cancellationToken);
    }

    private async Task<(decimal Deposit, decimal Wallet)> BuildSnapshotsAsync(
        SubscriberProfile profile,
        string? msisdn,
        CancellationToken cancellationToken)
    {
        var normalized = Customer360WalletBuilder.NormalizeMsisdn(msisdn) ?? msisdn ?? string.Empty;
        var wallet = profile.PrepaidBalance
                     ?? (string.IsNullOrEmpty(normalized) ? 0m : Customer360WalletBuilder.SimulateBalance(normalized));

        decimal deposit = 0m;
        if (!string.IsNullOrEmpty(normalized))
        {
            var hash = Math.Abs(normalized.GetHashCode(StringComparison.Ordinal));
            deposit = hash % 80_001m + 20_000m;
        }

        if (!string.IsNullOrEmpty(normalized))
        {
            try
            {
                var outstanding = await _billing.GetOutstandingBalanceAsync(normalized, cancellationToken);
                if (outstanding < 0)
                {
                    deposit = Math.Max(deposit, -outstanding);
                }
            }
            catch
            {
                // demo CBS may be unavailable — keep simulated deposit
            }
        }

        return (deposit, wallet);
    }

    private async Task EnsureNoBlockingRefundAsync(
        string msisdnAssetId,
        string? excludeOperationId,
        CancellationToken cancellationToken)
    {
        var open = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(
                o => o.Kind == TelecomOperationKind.DepositRefundSettlement
                     && o.MsisdnAssetId == msisdnAssetId
                     && BlockingStatuses.Contains(o.Status)
                     && (excludeOperationId == null || o.Id != excludeOperationId),
                cancellationToken);

        if (open)
        {
            throw new BusinessRuleViolationException("VAL-15-05: يوجد طلب استرداد مالي مفتوح على نفس الخط.");
        }
    }

    private static void EnsureCustomerEligible(Customer? customer)
    {
        if (customer == null)
        {
            throw new BusinessRuleViolationException("العميل غير موجود.");
        }

        if (customer.Status == CustomerStatus.Blacklisted)
        {
            throw new BusinessRuleViolationException("VAL-15-03: العميل على القائمة السوداء — لا يمكن الاسترداد.");
        }

        if (customer.Status == CustomerStatus.Closed)
        {
            throw new BusinessRuleViolationException("VAL-15-03: حساب العميل مغلق.");
        }
    }

    private static RefundEligibilityResult Deny(
        string messageAr,
        string? customerId,
        string? msisdn,
        string? msisdnAssetId,
        decimal depositSnap,
        decimal walletSnap,
        bool requiresDual,
        bool requiresBo,
        string outcomeCode) =>
        new(false, messageAr, customerId, msisdn, msisdnAssetId, depositSnap, walletSnap, requiresDual, requiresBo, outcomeCode);
}
