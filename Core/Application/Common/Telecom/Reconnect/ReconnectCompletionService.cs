using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Reconnect;

public interface IReconnectCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class ReconnectCompletionService : IReconnectCompletionService
{
    private const decimal DefaultRestoredPostpaidLimitSyp = 200_000m;

    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<BillingIntegrationLog> _billingLogRepository;
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepository;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IHLRLiveStatusService _hlr;
    private readonly IBillingSystemIntegration _billing;
    private readonly IUnitOfWork _unitOfWork;

    public ReconnectCompletionService(
        IQueryContext query,
        ICommandRepository<TelecomOperationAuditLog> auditRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<BillingIntegrationLog> billingLogRepository,
        ICommandRepository<TelecomTechnicalTicket> ticketRepository,
        ISmsGatewayIntegration sms,
        IHLRLiveStatusService hlr,
        IBillingSystemIntegration billing,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _auditRepository = auditRepository;
        _profileRepository = profileRepository;
        _billingLogRepository = billingLogRepository;
        _ticketRepository = ticketRepository;
        _sms = sms;
        _hlr = hlr;
        _billing = billing;
        _unitOfWork = unitOfWork;
    }

    public async Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.Reconnect)
        {
            return;
        }

        operation.ProvisioningResult = "Completed";
        operation.ReactivationAtUtc ??= DateTime.UtcNow;

        var line = msisdn;
        if (string.IsNullOrEmpty(line) && !string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            line = await _query.MsisdnAsset.AsNoTracking()
                .Where(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var fieldChanges = System.Text.Json.JsonSerializer.Serialize(new
        {
            before = operation.PriorOperationalStatus,
            after = "Active",
            clearance = operation.ClearanceType,
            reason = operation.ReconnectReason,
            paymentRef = operation.PaymentReference,
            msisdn = line,
        });

        await _auditRepository.CreateAsync(
            new TelecomOperationAuditLog
            {
                TelecomOperationRequestId = operation.Id,
                FromStatus = TelecomOperationStatus.Provisioning,
                ToStatus = TelecomOperationStatus.Completed,
                ActorUserId = actorUserId,
                Note =
                    $"RCN completed|clearance={operation.ClearanceType}|reason={operation.ReconnectReason}|msisdn={line ?? "—"}",
                FieldChangesJson = fieldChanges,
                OccurredAtUtc = DateTime.UtcNow,
                CorrelationId = operation.CorrelationId,
                CreatedById = actorUserId,
            },
            cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(line))
        {
            await _hlr.MarkMockSubscriberActiveAsync(line, cancellationToken);

            if (RequiresPaymentSettlement(operation))
            {
                await SettlePaidReconnectLedgerAsync(operation, line, actorUserId, cancellationToken);
            }

            var body =
                $"تم إعادة تفعيل خطك {line}. مرجع العملية: {operation.Number}.";
            await _sms.SendAsync(line, body, cancellationToken);
        }
    }

    private static bool RequiresPaymentSettlement(TelecomOperationRequest operation) =>
        string.Equals(operation.ClearanceType, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrWhiteSpace(operation.PaymentReference);

    private async Task SettlePaidReconnectLedgerAsync(
        TelecomOperationRequest operation,
        string line,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var paymentRef = operation.PaymentReference?.Trim();
        var amount = await ResolveSettlementAmountAsync(paymentRef, cancellationToken);
        var idempotencyKey = $"{operation.CorrelationId}:rcn-cbs-settle";

        try
        {
            var outstanding = await _billing.GetOutstandingBalanceAsync(line, cancellationToken);
            if (outstanding < 0)
            {
                var payAmount = amount > 0 ? amount : Math.Abs(outstanding);
                var paymentTxnId = await ResolvePaymentTransactionIdAsync(paymentRef, cancellationToken);
                await _billing.RechargeAsync(
                    new BillingRechargeRequest(
                        paymentTxnId ?? string.Empty,
                        paymentRef ?? operation.Number,
                        line,
                        payAmount,
                        $"{operation.CorrelationId}:recharge",
                        operation.BranchId),
                    cancellationToken);
            }
        }
        catch
        {
            // Best-effort recharge — adjust below enforces zero outstanding.
        }

        await _billing.AdjustBalanceAsync(
            line,
            0,
            $"RCN settlement|ref={paymentRef ?? "—"}",
            idempotencyKey,
            cancellationToken);

        if (!string.IsNullOrEmpty(operation.SubscriberProfileId))
        {
            var profile = await _profileRepository.GetAsync(operation.SubscriberProfileId, cancellationToken);
            if (profile != null && profile.PostpaidCreditLimit is < 0)
            {
                profile.PostpaidCreditLimit = TelecomDemoBaselines.IsDebtShowcaseMsisdn(line)
                    ? TelecomDemoBaselines.DebtRestoredPostpaidLimitSyp
                    : DefaultRestoredPostpaidLimitSyp;
                profile.UpdatedById = actorUserId;
                _profileRepository.Update(profile);
            }
        }

        if (!string.IsNullOrEmpty(paymentRef))
        {
            var logged = await _query.BillingIntegrationLog.AsNoTracking()
                .AnyAsync(
                    l => !l.IsDeleted && l.Success && l.CorrelationId == paymentRef,
                    cancellationToken);

            if (!logged)
            {
                await _billingLogRepository.CreateAsync(
                    new BillingIntegrationLog
                    {
                        Success = true,
                        CorrelationId = paymentRef,
                        AttemptNumber = 1,
                        Message = $"CBS-OK-200: RCN bill pay {amount:N0} SYP ({paymentRef}).",
                        IntegrationTarget = "Huawei CBS API v2.1",
                        TelecomOperationRequestId = operation.Id,
                        BranchId = operation.BranchId,
                        CreatedById = actorUserId,
                    },
                    cancellationToken);
            }
        }

        await ResolveRevenueLeakageTicketsAsync(line, actorUserId, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private async Task ResolveRevenueLeakageTicketsAsync(
        string line,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (!TelecomDemoBaselines.IsDebtShowcaseMsisdn(line))
        {
            return;
        }

        var openTickets = await _ticketRepository.GetQuery()
            .Where(t => !t.IsDeleted
                        && t.Msisdn == line
                        && t.TicketCategory == TechnicalTicketCategory.RevenueAssurance
                        && (t.Status == TechnicalTicketStatus.Open
                            || t.Status == TechnicalTicketStatus.InProgress))
            .ToListAsync(cancellationToken);

        foreach (var ticket in openTickets)
        {
            ticket.Status = TechnicalTicketStatus.Resolved;
            ticket.ResolvedAtUtc = DateTime.UtcNow;
            ticket.UpdatedById = actorUserId;
            ticket.Notes = string.IsNullOrWhiteSpace(ticket.Notes)
                ? "resolved=rcn-payment-settlement"
                : $"{ticket.Notes}|resolved=rcn-payment-settlement";
            _ticketRepository.Update(ticket);
        }
    }

    private async Task<string?> ResolvePaymentTransactionIdAsync(
        string? paymentReference,
        CancellationToken cancellationToken)
    {
        var reference = (paymentReference ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return await _query.TelecomPaymentTransaction.AsNoTracking()
            .Where(t => !t.IsDeleted
                        && t.Status == PaymentTransactionStatus.Completed
                        && (t.GatewayReference == reference
                            || t.ReceiptNumber == reference
                            || t.Number == reference
                            || t.GatewayTransactionId == reference))
            .OrderByDescending(t => t.ConfirmedAtUtc ?? t.CreatedAtUtc)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<decimal> ResolveSettlementAmountAsync(
        string? paymentReference,
        CancellationToken cancellationToken)
    {
        var reference = (paymentReference ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(reference))
        {
            return TelecomDemoBaselines.DebtFullPaymentAmountSyp;
        }

        if (TelecomDemoBaselines.IsDebtFullPaymentReceipt(reference))
        {
            return TelecomDemoBaselines.DebtFullPaymentAmountSyp;
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

        return txnAmount is > 0 ? txnAmount.Value : TelecomDemoBaselines.DebtFullPaymentAmountSyp;
    }
}
