using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.Suspension;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.BackOffice;

public sealed class BackOfficePaymentReferenceValidator : IBackOfficePaymentReferenceValidator
{
    private readonly IQueryContext _query;
    private readonly IBillingSystemIntegration _billing;

    public BackOfficePaymentReferenceValidator(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<BackOfficePaymentValidationResult> ValidateReconnectPaymentAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.Reconnect)
        {
            return new BackOfficePaymentValidationResult(true, "لا ينطبق التحقق المالي.");
        }

        var clearance = (operation.ClearanceType ?? string.Empty).Trim();

        if (string.Equals(clearance, ReconnectWellKnown.Operational, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clearance, ReconnectWellKnown.Fraud, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clearance, ReconnectWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase))
        {
            return new BackOfficePaymentValidationResult(true, "لا يتطلب تسوية مالية لهذا مسار التسوية.");
        }

        var requiresPayment = string.Equals(clearance, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase);

        if (!requiresPayment
            && string.Equals(clearance, ReconnectWellKnown.Customer, StringComparison.OrdinalIgnoreCase))
        {
            var sourceSuspensionType = await ResolveSourceSuspensionTypeAsync(operation, cancellationToken);
            requiresPayment = string.Equals(sourceSuspensionType, SuspensionWellKnown.Billing, StringComparison.OrdinalIgnoreCase);
        }

        if (!requiresPayment)
        {
            return new BackOfficePaymentValidationResult(true, "لا يتطلب تسوية مالية.");
        }

        var paymentRef = operation.PaymentReference?.Trim();
        if (string.IsNullOrEmpty(paymentRef))
        {
            return new BackOfficePaymentValidationResult(
                false,
                "VAL-09-02: مرجع الدفع مطلوب قبل اعتماد إعادة التفعيل.",
                "PaymentReferenceRequired");
        }

        var msisdn = await ResolveMsisdnAsync(operation, cancellationToken);
        if (!string.IsNullOrEmpty(msisdn))
        {
            var outstanding = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
            if (outstanding < 0)
            {
                return new BackOfficePaymentValidationResult(
                    false,
                    $"VAL-09-02: ذمم مالية بقيمة {-outstanding:N0} ل.س — يجب التسوية قبل الاعتماد.",
                    "OutstandingDebt");
            }
        }

        var journalHit = await _query.TelecomPaymentTransaction.AsNoTracking()
            .AnyAsync(
                t => !t.IsDeleted
                     && t.Status == PaymentTransactionStatus.Completed
                     && (t.GatewayReference == paymentRef
                         || t.ReceiptNumber == paymentRef
                         || t.Number == paymentRef
                         || t.GatewayTransactionId == paymentRef),
                cancellationToken);

        if (!journalHit)
        {
            var billingLogHit = await _query.BillingIntegrationLog.AsNoTracking()
                .AnyAsync(
                    l => !l.IsDeleted
                         && l.Success
                         && (l.CorrelationId == paymentRef
                             || (l.RequestPayload != null && l.RequestPayload.Contains(paymentRef))),
                    cancellationToken);

            if (!billingLogHit)
            {
                return new BackOfficePaymentValidationResult(
                    false,
                    "VAL-09-02: مرجع الدفع غير موجود في سجل الفوترة — راجع الكاشير أو CBS.",
                    "PaymentReferenceNotFound");
            }
        }

        return new BackOfficePaymentValidationResult(true, "تم التحقق من مرجع الدفع في سجل الفوترة.");
    }

    private async Task<string?> ResolveSourceSuspensionTypeAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(operation.SourceSuspensionOperationId))
        {
            return null;
        }

        return await _query.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Id == operation.SourceSuspensionOperationId)
            .Select(o => o.SuspensionType)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> ResolveMsisdnAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (operation.MsisdnAsset != null)
        {
            return operation.MsisdnAsset.Msisdn;
        }

        if (string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            return null;
        }

        return await _query.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId)
            .Select(m => m.Msisdn)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
