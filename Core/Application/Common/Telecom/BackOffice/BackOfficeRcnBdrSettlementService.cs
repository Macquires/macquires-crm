using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.Reconnect;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.BackOffice;

public sealed class BackOfficeRcnBdrSettlementService : IBackOfficeRcnBdrSettlementService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackOfficePaymentReferenceValidator _paymentValidator;
    private readonly NumberSequenceService _numberSequence;

    public BackOfficeRcnBdrSettlementService(
        IQueryContext query,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IUnitOfWork unitOfWork,
        IBackOfficePaymentReferenceValidator paymentValidator,
        NumberSequenceService numberSequence)
    {
        _query = query;
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
        _paymentValidator = paymentValidator;
        _numberSequence = numberSequence;
    }

    public async Task EnsureAutoSettlementForPaidReconnectAsync(
        TelecomOperationRequest reconnectOperation,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (reconnectOperation.Kind != TelecomOperationKind.Reconnect)
        {
            return;
        }

        if (!string.Equals(
                reconnectOperation.ClearanceType,
                ReconnectWellKnown.Payment,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var paymentCheck = await _paymentValidator.ValidateReconnectPaymentAsync(reconnectOperation, cancellationToken);
        if (!paymentCheck.IsValid)
        {
            return;
        }

        var assetId = reconnectOperation.MsisdnAssetId;
        if (string.IsNullOrEmpty(assetId))
        {
            return;
        }

        var collectedAmount = await ResolveCollectedAmountAsync(reconnectOperation, cancellationToken);
        await SupersedeManualWriteOffRequestsAsync(assetId, actorUserId, reconnectOperation.Number, cancellationToken);
        await EnsureAutoSettlementOperationAsync(
            reconnectOperation,
            assetId,
            collectedAmount,
            actorUserId,
            cancellationToken);
    }

    private async Task SupersedeManualWriteOffRequestsAsync(
        string msisdnAssetId,
        string actorUserId,
        string? rcnNumber,
        CancellationToken cancellationToken)
    {
        var pendingManual = await _operationRepository.GetQuery()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.BadDebtRecovery
                        && o.MsisdnAssetId == msisdnAssetId
                        && (o.Status == TelecomOperationStatus.PendingDocuments
                            || o.Status == TelecomOperationStatus.In_Progress
                            || o.Status == TelecomOperationStatus.Approved_Pending_Cash))
            .ToListAsync(cancellationToken);

        foreach (var bdr in pendingManual.Where(BackOfficeBdrLedgerResolver.IsManualWriteOffRequest))
        {
            bdr.Status = TelecomOperationStatus.Failed;
            bdr.CollectionSettlementStatus = BadDebtWellKnown.SettlementFailed;
            bdr.UpdatedById = actorUserId;
            bdr.Notes = AppendNote(
                bdr.Notes,
                $"{BackOfficeBdrLedgerResolver.SupersededByFullPaymentMarker}|rcn={rcnNumber ?? "—"}");
            _operationRepository.Update(bdr);
        }
    }

    private async Task EnsureAutoSettlementOperationAsync(
        TelecomOperationRequest reconnectOperation,
        string msisdnAssetId,
        decimal collectedAmount,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        var existing = await _operationRepository.GetQuery()
            .FirstOrDefaultAsync(
                o => !o.IsDeleted
                     && o.Kind == TelecomOperationKind.BadDebtRecovery
                     && o.MsisdnAssetId == msisdnAssetId
                     && o.Notes != null
                     && o.Notes.Contains(BackOfficeBdrLedgerResolver.AutoSettlementNoteMarker),
                cancellationToken);

        if (existing == null)
        {
            var (entityName, prefix) = ("TelecomOp_BadDebt", "BDR-");
            var number = await _numberSequence.GenerateNumberAsync(
                entityName,
                prefix,
                "",
                useDate: false,
                cancellationToken: cancellationToken);

            existing = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.BadDebtRecovery,
                Number = number,
                CorrelationId = Guid.CreateVersion7().ToString(),
                Status = TelecomOperationStatus.Completed,
                ApprovalLevelRequired = "BackOffice",
                DocumentStatus = TelecomDocumentStatus.Verified,
                SubscriberProfileId = reconnectOperation.SubscriberProfileId,
                MsisdnAssetId = msisdnAssetId,
                CollectionAction = BadDebtWellKnown.PaymentRecorded,
                DunningStage = BadDebtWellKnown.Settled,
                PriorDunningStage = BadDebtWellKnown.WriteOffPending,
                OutstandingBalanceSnapshot = 0m,
                WriteOffAmount = 0m,
                CollectedAmount = collectedAmount,
                PaymentReference = reconnectOperation.PaymentReference,
                CollectionSettlementStatus = BadDebtWellKnown.SettlementCompleted,
                CollectionNote = "تسوية آلية — إغلاق ذمة Bad Debt بعد الدفع الكامل بالمعرض.",
                ProvisioningResult = $"AUTO-BDR-{number}",
                FraudClearanceConfirmed = true,
                FraudClearanceByUserId = actorUserId,
                ConfirmedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                BranchId = reconnectOperation.BranchId,
                Notes =
                    $"{BackOfficeBdrLedgerResolver.AutoSettlementNoteMarker}|rcn={reconnectOperation.Number}|ref={reconnectOperation.PaymentReference}|cash={collectedAmount:0}",
            };

            await _operationRepository.CreateAsync(existing);
            return;
        }

        existing.Status = TelecomOperationStatus.Completed;
        existing.CollectionAction = BadDebtWellKnown.PaymentRecorded;
        existing.OutstandingBalanceSnapshot = 0m;
        existing.WriteOffAmount = 0m;
        existing.CollectedAmount = collectedAmount;
        existing.PaymentReference = reconnectOperation.PaymentReference;
        existing.CollectionSettlementStatus = BadDebtWellKnown.SettlementCompleted;
        existing.DunningStage = BadDebtWellKnown.Settled;
        existing.ConfirmedAtUtc = DateTime.UtcNow;
        existing.UpdatedById = actorUserId;
        existing.Notes = AppendNote(
            existing.Notes,
            $"rcn={reconnectOperation.Number}|ref={reconnectOperation.PaymentReference}|cash={collectedAmount:0}");
        _operationRepository.Update(existing);
    }

    private async Task<decimal> ResolveCollectedAmountAsync(
        TelecomOperationRequest reconnectOperation,
        CancellationToken cancellationToken)
    {
        var reference = (reconnectOperation.PaymentReference ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(reference))
        {
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
                return txnAmount.Value;
            }

            if (TelecomDemoBaselines.IsDebtFullPaymentReceipt(reference))
            {
                return TelecomDemoBaselines.DebtFullPaymentAmountSyp;
            }
        }

        return TelecomDemoBaselines.DebtFullPaymentAmountSyp;
    }

    private static string AppendNote(string? existing, string suffix)
    {
        var baseNote = (existing ?? string.Empty).Trim();
        return string.IsNullOrEmpty(baseNote) ? suffix : $"{baseNote}|{suffix}";
    }
}
