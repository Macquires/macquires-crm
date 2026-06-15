using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Refund;

public sealed class RefundCompletionService : IRefundCompletionService
{
    private readonly IQueryContext _query;
    private readonly IBillingSystemIntegration _billing;
    private readonly IBillingPostingIntegration _billingPosting;
    private readonly IPaymentGatewayIntegration _paymentGateway;
    private readonly ISmsGatewayIntegration _sms;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;

    public RefundCompletionService(
        IQueryContext query,
        IBillingSystemIntegration billing,
        IBillingPostingIntegration billingPosting,
        IPaymentGatewayIntegration paymentGateway,
        ISmsGatewayIntegration sms,
        ICommandRepository<TelecomOperationRequest> operationRepository)
    {
        _query = query;
        _billing = billing;
        _billingPosting = billingPosting;
        _paymentGateway = paymentGateway;
        _sms = sms;
        _operationRepository = operationRepository;
    }

    public async Task FulfillAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.DepositRefundSettlement)
        {
            return;
        }

        var amount = operation.RefundAmount ?? 0m;
        if (amount <= 0)
        {
            throw new InvalidOperationException("مبلغ الاسترداد غير صالح.");
        }

        var msisdn = await ResolveMsisdnAsync(operation, cancellationToken);
        operation.RefundSettlementStatus = RefundWellKnown.SettlementPending;

        var cbsResult = await _billing.ProvisionAsync(
            new BillingProvisionRequest(
                operation.Id,
                operation.Number,
                msisdn,
                TelecomOperationKind.DepositRefundSettlement,
                operation.CorrelationId,
                amount,
                ProductServiceCode: TelecomBssOperations.CbsPostRefundCreditNote,
                BranchId: operation.BranchId),
            cancellationToken);

        if (!cbsResult.Success)
        {
            operation.RefundSettlementStatus = RefundWellKnown.SettlementFailed;
            throw new InvalidOperationException(cbsResult.Message ?? "فشل ترحيل إشعار دائن CBS للاسترداد.");
        }

        operation.RefundCbsReference = cbsResult.Message ?? $"CBS-RFD-{operation.Number}";

        var journal = await _billingPosting.PostJournalEntryAsync(
            new BillingJournalPostRequest(
                operation.Id,
                operation.Number,
                msisdn,
                TelecomOperationKind.DepositRefundSettlement,
                amount,
                TelecomBssOperations.CbsPostRefundCreditNote,
                operation.CorrelationId,
                operation.BranchId),
            cancellationToken);

        if (journal.Success && !string.IsNullOrEmpty(journal.JournalEntryId))
        {
            operation.RefundCbsReference = journal.JournalEntryId;
        }

        if (string.Equals(operation.RefundMethod, RefundWellKnown.MethodWalletCredit, StringComparison.OrdinalIgnoreCase)
            || string.Equals(operation.RefundType, RefundWellKnown.TypeSyriatelCash, StringComparison.OrdinalIgnoreCase))
        {
            var walletResult = await _paymentGateway.RefundToWalletAsync(
                new WalletRefundRequest(
                    operation.Id,
                    operation.Number,
                    msisdn,
                    amount,
                    operation.CorrelationId),
                cancellationToken);

            if (!walletResult.Success)
            {
                await _billing.ReverseProvisionAsync(
                    new BillingProvisionRequest(
                        operation.Id,
                        operation.Number,
                        msisdn,
                        TelecomOperationKind.DepositRefundSettlement,
                        operation.CorrelationId,
                        amount,
                        ProductServiceCode: TelecomBssOperations.CbsPostRefundCreditNote,
                BranchId: operation.BranchId),
                    cancellationToken);
                operation.RefundSettlementStatus = RefundWellKnown.SettlementFailed;
                throw new InvalidOperationException(walletResult.Message ?? "فشل إيداع المحفظة.");
            }

            operation.RefundGatewayReference = walletResult.GatewayTransactionId;
        }

        operation.RefundSettlementStatus = RefundWellKnown.SettlementSettled;
        operation.ProvisioningResult = "Settled";
        operation.Notes = AppendAuditSnapshot(operation);

        if (!string.IsNullOrEmpty(msisdn))
        {
            await _sms.SendAsync(
                msisdn,
                $"تم استرداد {amount:N0} ل.س — مرجع {operation.Number}.",
                cancellationToken);
        }

        operation.UpdatedById = actorUserId;
        _operationRepository.Update(operation);
    }

    public async Task CompensateOnFailureAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.DepositRefundSettlement
            || string.IsNullOrEmpty(operation.RefundCbsReference))
        {
            return;
        }

        var msisdn = await ResolveMsisdnAsync(operation, cancellationToken);
        await _billing.ReverseProvisionAsync(
            new BillingProvisionRequest(
                operation.Id,
                operation.Number,
                msisdn,
                TelecomOperationKind.DepositRefundSettlement,
                operation.CorrelationId,
                operation.RefundAmount,
                ProductServiceCode: TelecomBssOperations.CbsPostRefundCreditNote,
                BranchId: operation.BranchId),
            cancellationToken);

        operation.RefundSettlementStatus = RefundWellKnown.SettlementFailed;
        _operationRepository.Update(operation);
    }

    private static string AppendAuditSnapshot(TelecomOperationRequest operation)
    {
        var payload = new
        {
            operation.RefundType,
            operation.RefundMethod,
            operation.RefundAmount,
            operation.DepositBalanceSnapshot,
            operation.WalletBalanceSnapshot,
            operation.RefundCbsReference,
            operation.RefundGatewayReference,
            operation.RefundSettlementStatus,
            SettledAtUtc = DateTime.UtcNow,
        };
        var json = JsonSerializer.Serialize(payload);
        return string.IsNullOrEmpty(operation.Notes) ? json : $"{operation.Notes}\n{json}";
    }

    private async Task<string> ResolveMsisdnAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (operation.MsisdnAsset?.Msisdn is { Length: > 0 } direct)
        {
            return direct;
        }

        if (string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            return string.Empty;
        }

        return await _query.MsisdnAsset.AsNoTracking()
                   .Where(m => m.Id == operation.MsisdnAssetId)
                   .Select(m => m.Msisdn)
                   .FirstOrDefaultAsync(cancellationToken)
               ?? string.Empty;
    }
}
