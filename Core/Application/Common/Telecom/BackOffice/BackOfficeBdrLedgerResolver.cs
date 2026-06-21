using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.Reconnect;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.BackOffice;

public sealed class BackOfficeBdrLedgerResolver : IBackOfficeBdrLedgerResolver
{
    public const string AutoSettlementNoteMarker = "auto=FullPaymentSettlement";
    public const string SupersededByFullPaymentMarker = "superseded=full-payment-rcn-path";
    public const string ManualWriteOffDemoMarker = "demo=WriteOffPartial";

    private readonly IQueryContext _query;
    private readonly IBillingSystemIntegration _billing;
    private readonly IBackOfficePaymentReferenceValidator _paymentValidator;

    public BackOfficeBdrLedgerResolver(
        IQueryContext query,
        IBillingSystemIntegration billing,
        IBackOfficePaymentReferenceValidator paymentValidator)
    {
        _query = query;
        _billing = billing;
        _paymentValidator = paymentValidator;
    }

    public static bool IsManualWriteOffRequest(TelecomOperationRequest operation)
    {
        var action = (operation.CollectionAction ?? string.Empty).Trim();
        return string.Equals(action, BadDebtWellKnown.WriteOffPartial, StringComparison.OrdinalIgnoreCase)
               || string.Equals(action, BadDebtWellKnown.WriteOffFull, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAutoSettlementRequest(TelecomOperationRequest operation) =>
        (operation.Notes ?? string.Empty).Contains(AutoSettlementNoteMarker, StringComparison.OrdinalIgnoreCase)
        || (string.Equals(operation.CollectionAction, BadDebtWellKnown.PaymentRecorded, StringComparison.OrdinalIgnoreCase)
            && (operation.WriteOffAmount ?? 0m) <= 0m
            && (operation.OutstandingBalanceSnapshot ?? -1m) == 0m);

    public async Task<bool> ShouldSuppressManualBdrInQueueAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.BadDebtRecovery)
        {
            return false;
        }

        if (!IsManualWriteOffRequest(operation))
        {
            return false;
        }

        if (string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            return false;
        }

        return await HasActivePaidReconnectClearanceAsync(operation.MsisdnAssetId, cancellationToken);
    }

    public async Task<BackOfficeBdrLedgerView> ResolveAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.BadDebtRecovery)
        {
            return new BackOfficeBdrLedgerView(0, 0, 0, BackOfficeBdrLedgerModes.StandardCollection, null);
        }

        if (IsAutoSettlementRequest(operation)
            || await IsLinkedToVerifiedFullPaymentAsync(operation, cancellationToken))
        {
            var cash = await ResolveVerifiedCashAmountAsync(operation, cancellationToken);
            return new BackOfficeBdrLedgerView(
                0,
                0,
                cash,
                BackOfficeBdrLedgerModes.FullPaymentSettlement,
                "تسوية آلية — الدين مغطى بالكامل بوصل الدفع المعتمد (لا خصم ولا إعفاء يدوي).");
        }

        if (IsManualWriteOffRequest(operation))
        {
            var outstanding = Math.Abs(operation.OutstandingBalanceSnapshot ?? 0m);
            return new BackOfficeBdrLedgerView(
                outstanding,
                operation.WriteOffAmount ?? 0m,
                operation.CollectedAmount ?? 0m,
                BackOfficeBdrLedgerModes.ManualDiscount,
                "طلب تسوية يدوي — خصم/إعفاء مالي يتطلب اعتماد اللجنة قبل التحصيل.");
        }

        var liveOutstanding = await ResolveLiveOutstandingDebtAsync(operation, cancellationToken);
        return new BackOfficeBdrLedgerView(
            liveOutstanding,
            operation.WriteOffAmount ?? 0m,
            operation.CollectedAmount ?? 0m,
            BackOfficeBdrLedgerModes.StandardCollection,
            null);
    }

    private async Task<bool> IsLinkedToVerifiedFullPaymentAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            return false;
        }

        if (!await HasActivePaidReconnectClearanceAsync(operation.MsisdnAssetId, cancellationToken))
        {
            return false;
        }

        var rcn = await FindActivePaidReconnectAsync(operation.MsisdnAssetId, cancellationToken);
        if (rcn == null)
        {
            return false;
        }

        var paymentCheck = await _paymentValidator.ValidateReconnectPaymentAsync(rcn, cancellationToken);
        return paymentCheck.IsValid;
    }

    private async Task<decimal> ResolveVerifiedCashAmountAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (operation.CollectedAmount is > 0)
        {
            return operation.CollectedAmount.Value;
        }

        if (!string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            var rcn = await FindActivePaidReconnectAsync(operation.MsisdnAssetId, cancellationToken);
            if (rcn != null)
            {
                var payment = await FindCompletedPaymentAmountAsync(rcn.PaymentReference, cancellationToken);
                if (payment is > 0)
                {
                    return payment.Value;
                }
            }
        }

        return TelecomDemoBaselines.DebtFullPaymentAmountSyp;
    }

    private async Task<decimal> ResolveLiveOutstandingDebtAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        var msisdn = operation.MsisdnAsset?.Msisdn;
        if (string.IsNullOrEmpty(msisdn) && !string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            msisdn = await _query.MsisdnAsset.AsNoTracking()
                .Where(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(msisdn))
        {
            return Math.Abs(operation.OutstandingBalanceSnapshot ?? 0m);
        }

        var balance = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
        return balance < 0 ? Math.Abs(balance) : 0m;
    }

    private async Task<bool> HasActivePaidReconnectClearanceAsync(
        string msisdnAssetId,
        CancellationToken cancellationToken)
    {
        return await _query.TelecomOperationRequest.AsNoTracking()
            .AnyAsync(
                o => !o.IsDeleted
                     && o.Kind == TelecomOperationKind.Reconnect
                     && o.MsisdnAssetId == msisdnAssetId
                     && (o.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance
                         || o.Status == TelecomOperationStatus.PendingDocuments)
                     && string.Equals(o.ClearanceType, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(o.PaymentReference),
                cancellationToken);
    }

    private async Task<TelecomOperationRequest?> FindActivePaidReconnectAsync(
        string msisdnAssetId,
        CancellationToken cancellationToken)
    {
        return await _query.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.Reconnect
                        && o.MsisdnAssetId == msisdnAssetId
                        && (o.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance
                            || o.Status == TelecomOperationStatus.PendingDocuments)
                        && string.Equals(o.ClearanceType, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<decimal?> FindCompletedPaymentAmountAsync(
        string? paymentReference,
        CancellationToken cancellationToken)
    {
        var reference = (paymentReference ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        var txnAmount = await _query.TelecomPaymentTransaction.AsNoTracking()
            .Where(t => !t.IsDeleted
                        && t.Status == PaymentTransactionStatus.Completed
                        && (t.GatewayReference == reference
                            || t.ReceiptNumber == reference
                            || t.Number == reference
                            || t.GatewayTransactionId == reference))
            .OrderByDescending(t => t.ConfirmedAtUtc ?? t.CreatedAtUtc)
            .Select(t => (decimal?)t.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        if (txnAmount is > 0)
        {
            return txnAmount;
        }

        if (TelecomDemoBaselines.IsDebtFullPaymentReceipt(reference))
        {
            return TelecomDemoBaselines.DebtFullPaymentAmountSyp;
        }

        return null;
    }
}
