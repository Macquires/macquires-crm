using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Telecom;
using Application.Common.Telecom.SellingLine;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.Suspension;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetTelecomOperationDetailDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public TelecomOperationKind Kind { get; init; }
    public string? KindLabelAr { get; init; }
    public TelecomOperationStatus Status { get; init; }
    public string? StatusLabelAr { get; init; }
    public TelecomDocumentStatus DocumentStatus { get; init; }
    public string? Msisdn { get; init; }
    public string? CurrentOwnerName { get; init; }
    public string? NewOwnerName { get; init; }
    public string? Notes { get; init; }
    public bool HasIdentityDocument { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public string? CorrelationId { get; init; }
    public ActivationChannel ActivationChannel { get; init; }
    public string? ActivationChannelLabelAr { get; init; }
    public string? DealerCode { get; init; }
    public string? BranchId { get; init; }
    public string? PaymentReference { get; init; }
    public decimal? InitialDepositAmount { get; init; }
    public string? OverrideReasonCode { get; init; }
    public DateTime? KycVerifiedAtUtc { get; init; }
    public string? ProvisioningStatusLabelAr { get; init; }
    public string? OfferCode { get; init; }
    public string? Imsi { get; init; }
    public string? Iccid { get; init; }
    public List<TelecomOperationAuditTrailItemDto> AuditTrail { get; init; } = new();
    public string? SourceSubscriptionTypeLabel { get; init; }
    public string? TargetSubscriptionTypeLabel { get; init; }
    public string? GsmMigrationReason { get; init; }
    public string? TransferReason { get; init; }
    public string? TakeOverObligationStatus { get; init; }
    public DepositTransferPolicy? DepositTransferPolicy { get; init; }
    public string? DepositTransferPolicyLabelAr { get; init; }
    public string? ApprovalLevelRequired { get; init; }
    public DateTime? TakeOverEffectiveDateUtc { get; init; }
    public string? ReplacementReason { get; init; }
    public bool IsLostOrStolenReport { get; init; }
    public string? PriorSimIccid { get; init; }
    public string? NewSimIccid { get; init; }
    public string? NumberChangeReason { get; init; }
    public string? PriorMsisdn { get; init; }
    public string? TargetMsisdn { get; init; }
    public decimal? PremiumFeeAmount { get; init; }
    public string? TerminationType { get; init; }
    public string? TerminationReason { get; init; }
    public DateTime? TerminationEffectiveDateUtc { get; init; }
    public decimal? FinalBillAmount { get; init; }
    public string? RetentionOfferOutcome { get; init; }
    public string? DeprovisionStatus { get; init; }
    public string? SuspensionType { get; init; }
    public string? SuspensionReason { get; init; }
    public string? ReconnectReason { get; init; }
    public string? ClearanceType { get; init; }
    public string? ClearanceTypeLabelAr { get; init; }
    public string? SourceSuspensionOperationId { get; init; }
    public string? SourceSuspensionType { get; init; }
    public string? SourceSuspensionTypeLabelAr { get; init; }
    public bool FraudClearanceConfirmed { get; init; }
    public string? FraudClearanceByUserId { get; init; }
    public string? CollectionAction { get; init; }
    public string? CollectionActionLabelAr { get; init; }
    public string? DunningStage { get; init; }
    public decimal? OutstandingBalanceSnapshot { get; init; }
    public decimal? CollectedAmount { get; init; }
    public decimal? WriteOffAmount { get; init; }
    public string? AgencyReference { get; init; }
    public string? CollectionSettlementStatus { get; init; }
    public string? CollectionNote { get; init; }
    public string? TechnicalTicketId { get; init; }
    public string? TechnicalTicketNumber { get; init; }
}

public record TelecomOperationAuditTrailItemDto(
    TelecomOperationStatus FromStatus,
    TelecomOperationStatus ToStatus,
    string? Note,
    DateTime OccurredAtUtc,
    string? CorrelationId);

public class GetTelecomOperationDetailResult
{
    public GetTelecomOperationDetailDto? Data { get; init; }
}

public class GetTelecomOperationDetailRequest : IRequest<GetTelecomOperationDetailResult>
{
    public string Id { get; init; } = null!;
}

public class GetTelecomOperationDetailHandler : IRequestHandler<GetTelecomOperationDetailRequest, GetTelecomOperationDetailResult>
{
    private readonly IQueryContext _context;
    private readonly IActivationChannelLabelProvider _channelLabels;

    public GetTelecomOperationDetailHandler(IQueryContext context, IActivationChannelLabelProvider channelLabels)
    {
        _context = context;
        _channelLabels = channelLabels;
    }

    public async Task<GetTelecomOperationDetailResult> Handle(
        GetTelecomOperationDetailRequest request,
        CancellationToken cancellationToken)
    {
        var op = await _context.TelecomOperationRequest
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.MsisdnAsset)
            .Include(x => x.SubscriberProfile!)
                .ThenInclude(p => p!.Customer)
            .Include(x => x.SecondarySubscriberProfile!)
                .ThenInclude(p => p!.Customer)
            .Include(x => x.SimInventory)
            .Include(x => x.Product)
            .Include(x => x.ProductOffering)
            .Include(x => x.AuditLogs)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (op == null)
        {
            return new GetTelecomOperationDetailResult();
        }

        var sourceLabel = await ResolveTypeLabelAsync(_context, op.SourceSubscriptionTypeId, cancellationToken);
        var targetLabel = await ResolveTypeLabelAsync(_context, op.TargetSubscriptionTypeId, cancellationToken);
        var priorSimIccid = await ResolveSimIccidAsync(_context, op.PriorSimInventoryId, cancellationToken);
        var priorMsisdn = await ResolveMsisdnAsync(_context, op.PriorMsisdnAssetId ?? op.MsisdnAssetId, cancellationToken);
        var targetMsisdn = await ResolveMsisdnAsync(_context, op.TargetMsisdnAssetId, cancellationToken);
        var sourceSuspensionType = await ResolveSourceSuspensionTypeAsync(
            op.SourceSuspensionOperationId,
            cancellationToken);

        var falloutTicket = await ResolveFalloutTicketAsync(op, cancellationToken);

        string? resolvedIccid = op.SimInventory?.Iccid;
        string? resolvedImsi = op.MsisdnAsset?.PairedImsi;
        if (op.MsisdnAsset != null)
        {
            var (kitIccid, kitImsi) = await MsisdnAssetKitResolver.ResolveForAssetAsync(
                _context,
                op.MsisdnAsset,
                op.SubscriberProfileId,
                op.SimInventoryId,
                cancellationToken);
            resolvedIccid = kitIccid ?? resolvedIccid;
            resolvedImsi = kitImsi ?? resolvedImsi;
        }

        return new GetTelecomOperationDetailResult
        {
            Data = new GetTelecomOperationDetailDto
            {
                Id = op.Id,
                Number = op.Number,
                Kind = op.Kind,
                KindLabelAr = TelecomOperationLabels.KindLabelAr(op.Kind),
                Status = op.Status,
                StatusLabelAr = TelecomOperationLabels.StatusLabelAr(op.Status),
                DocumentStatus = op.DocumentStatus,
                Msisdn = op.MsisdnAsset?.Msisdn,
                CurrentOwnerName = op.SubscriberProfile?.Customer?.DisplayName,
                NewOwnerName = op.SecondarySubscriberProfile?.Customer?.DisplayName,
                Notes = op.Notes,
                HasIdentityDocument = !string.IsNullOrWhiteSpace(op.IdentityDocumentStorageKey),
                CreatedAtUtc = op.CreatedAtUtc,
                CorrelationId = op.CorrelationId,
                ActivationChannel = op.ActivationChannel,
                ActivationChannelLabelAr = await _channelLabels.GetLabelArAsync(op.ActivationChannel, cancellationToken),
                DealerCode = op.DealerCode,
                BranchId = op.BranchId,
                PaymentReference = op.PaymentReference,
                InitialDepositAmount = op.InitialDepositAmount,
                OverrideReasonCode = op.OverrideReasonCode,
                KycVerifiedAtUtc = op.KycVerifiedAtUtc,
                ProvisioningStatusLabelAr = TelecomOperationLabels.StatusLabelAr(op.Status),
                OfferCode = op.Product?.ServiceCode ?? op.ProductOffering?.Code,
                Imsi = resolvedImsi,
                Iccid = resolvedIccid,
                NewSimIccid = resolvedIccid,
                PriorSimIccid = priorSimIccid,
                ReplacementReason = op.ReplacementReason,
                IsLostOrStolenReport = op.IsLostOrStolenReport,
                SourceSubscriptionTypeLabel = sourceLabel,
                TargetSubscriptionTypeLabel = targetLabel,
                GsmMigrationReason = op.GsmMigrationReason,
                TransferReason = op.TransferReason,
                TakeOverObligationStatus = op.TakeOverObligationStatus,
                DepositTransferPolicy = op.DepositTransferPolicy,
                DepositTransferPolicyLabelAr = DepositPolicyLabelAr(op.DepositTransferPolicy),
                ApprovalLevelRequired = op.ApprovalLevelRequired,
                TakeOverEffectiveDateUtc = op.TakeOverEffectiveDateUtc,
                NumberChangeReason = op.NumberChangeReason,
                PriorMsisdn = priorMsisdn,
                TargetMsisdn = targetMsisdn,
                PremiumFeeAmount = op.PremiumFeeAmount,
                TerminationType = op.TerminationType,
                TerminationReason = op.TerminationReason,
                TerminationEffectiveDateUtc = op.TerminationEffectiveDateUtc,
                FinalBillAmount = op.FinalBillAmount,
                RetentionOfferOutcome = op.RetentionOfferOutcome,
                DeprovisionStatus = op.DeprovisionStatus,
                SuspensionType = op.SuspensionType,
                SuspensionReason = op.SuspensionReason,
                ReconnectReason = op.ReconnectReason,
                ClearanceType = op.ClearanceType,
                ClearanceTypeLabelAr = ClearanceTypeLabelAr(op.ClearanceType),
                SourceSuspensionOperationId = op.SourceSuspensionOperationId,
                SourceSuspensionType = sourceSuspensionType,
                SourceSuspensionTypeLabelAr = SuspensionTypeLabelAr(sourceSuspensionType),
                FraudClearanceConfirmed = op.FraudClearanceConfirmed,
                FraudClearanceByUserId = op.FraudClearanceByUserId,
                CollectionAction = op.CollectionAction,
                CollectionActionLabelAr = CollectionActionLabelAr(op.CollectionAction),
                DunningStage = op.DunningStage,
                OutstandingBalanceSnapshot = op.OutstandingBalanceSnapshot,
                CollectedAmount = op.CollectedAmount,
                WriteOffAmount = op.WriteOffAmount,
                AgencyReference = op.AgencyReference,
                CollectionSettlementStatus = op.CollectionSettlementStatus,
                CollectionNote = op.CollectionNote,
                TechnicalTicketId = falloutTicket?.Id,
                TechnicalTicketNumber = falloutTicket?.TicketNumber,
                AuditTrail = op.AuditLogs
                    .OrderBy(a => a.OccurredAtUtc)
                    .Select(a => new TelecomOperationAuditTrailItemDto(
                        a.FromStatus,
                        a.ToStatus,
                        a.Note,
                        a.OccurredAtUtc,
                        a.CorrelationId))
                    .ToList()
            }
        };
    }

    private async Task<(string Id, string TicketNumber)?> ResolveFalloutTicketAsync(
        TelecomOperationRequest op,
        CancellationToken cancellationToken)
    {
        if (op.Status is not (TelecomOperationStatus.Failed or TelecomOperationStatus.PendingExternal))
        {
            return null;
        }

        var opId = op.Id;
        var ticket = await _context.TelecomTechnicalTicket.AsNoTracking()
            .Where(t => !t.IsDeleted
                        && t.TicketCategory == TechnicalTicketCategory.LineActivation
                        && t.PayloadJson != null
                        && t.PayloadJson.Contains(opId))
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new { t.Id, t.TicketNumber })
            .FirstOrDefaultAsync(cancellationToken);

        return ticket == null ? null : (ticket.Id, ticket.TicketNumber);
    }

    private static async Task<string?> ResolveTypeLabelAsync(
        IQueryContext context,
        string? typeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(typeId))
        {
            return null;
        }

        var row = await context.TelecomSubscriptionTypeLookup.AsNoTracking()
            .Where(t => !t.IsDeleted && t.Id == typeId)
            .Select(t => new { t.NameAr, t.Code })
            .FirstOrDefaultAsync(cancellationToken);

        return row == null ? typeId : $"{row.NameAr} ({row.Code})";
    }

    private static async Task<string?> ResolveMsisdnAsync(
        IQueryContext context,
        string? msisdnAssetId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(msisdnAssetId))
        {
            return null;
        }

        return await context.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted && m.Id == msisdnAssetId)
            .Select(m => m.Msisdn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static async Task<string?> ResolveSimIccidAsync(
        IQueryContext context,
        string? simInventoryId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(simInventoryId))
        {
            return null;
        }

        return await context.SimInventory.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Id == simInventoryId)
            .Select(s => s.Iccid)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> ResolveSourceSuspensionTypeAsync(
        string? sourceSuspensionOperationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sourceSuspensionOperationId))
        {
            return null;
        }

        return await _context.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.Id == sourceSuspensionOperationId)
            .Select(o => o.SuspensionType)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? ClearanceTypeLabelAr(string? clearanceType)
    {
        if (string.IsNullOrWhiteSpace(clearanceType))
        {
            return null;
        }

        if (string.Equals(clearanceType, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase))
        {
            return "تسوية مالية";
        }

        if (string.Equals(clearanceType, ReconnectWellKnown.Fraud, StringComparison.OrdinalIgnoreCase))
        {
            return "إزالة حظر احتيال";
        }

        if (string.Equals(clearanceType, ReconnectWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase))
        {
            return "تنظيمي";
        }

        if (string.Equals(clearanceType, ReconnectWellKnown.Operational, StringComparison.OrdinalIgnoreCase))
        {
            return "تشغيلي";
        }

        return "طلب عميل";
    }

    private static string? SuspensionTypeLabelAr(string? suspensionType)
    {
        if (string.IsNullOrWhiteSpace(suspensionType))
        {
            return null;
        }

        if (string.Equals(suspensionType, SuspensionWellKnown.Billing, StringComparison.OrdinalIgnoreCase))
        {
            return "حظر فواتير";
        }

        if (string.Equals(suspensionType, SuspensionWellKnown.Fraud, StringComparison.OrdinalIgnoreCase))
        {
            return "حظر احتيال";
        }

        if (string.Equals(suspensionType, SuspensionWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase))
        {
            return "حظر تنظيمي";
        }

        if (string.Equals(suspensionType, SuspensionWellKnown.Operational, StringComparison.OrdinalIgnoreCase))
        {
            return "حظر تشغيلي";
        }

        return "طلب عميل";
    }

    private static string? CollectionActionLabelAr(string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return null;
        }

        if (string.Equals(action, BadDebtWellKnown.PaymentRecorded, StringComparison.OrdinalIgnoreCase))
        {
            return "تسجيل دفعة تحصيل";
        }

        if (string.Equals(action, BadDebtWellKnown.PaymentPlan, StringComparison.OrdinalIgnoreCase))
        {
            return "خطة سداد";
        }

        if (string.Equals(action, BadDebtWellKnown.DunningEscalation, StringComparison.OrdinalIgnoreCase))
        {
            return "تصعيد تذكير";
        }

        if (string.Equals(action, BadDebtWellKnown.AgencyReferral, StringComparison.OrdinalIgnoreCase))
        {
            return "إحالة وكالة";
        }

        if (string.Equals(action, BadDebtWellKnown.WriteOffPartial, StringComparison.OrdinalIgnoreCase))
        {
            return "شطب جزئي";
        }

        if (string.Equals(action, BadDebtWellKnown.WriteOffFull, StringComparison.OrdinalIgnoreCase))
        {
            return "شطب كامل";
        }

        return action;
    }

    private static string? DepositPolicyLabelAr(DepositTransferPolicy? policy) => policy switch
    {
        DepositTransferPolicy.RetainWithOldOwner => "يبقى لدى المالك السابق",
        DepositTransferPolicy.TransferToNewOwner => "ينتقل للمالك الجديد",
        DepositTransferPolicy.Forfeit => "مصادرة",
        DepositTransferPolicy.RefundOldOwner => "استرداد للمالك السابق",
        null => null,
        _ => policy.ToString()
    };

}
