using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.BackOffice;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Queries;

public sealed record PendingBackOfficeOperationDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public TelecomOperationKind Kind { get; init; }
    public string? KindNameAr { get; init; }
    public string PipelineState { get; init; } = BackOfficeTelecomPipelineState.PendingBackOfficeApproval;
    public string? Msisdn { get; init; }
    public string? SubscriberName { get; init; }
    public string? SuspensionType { get; init; }
    public string? ClearanceType { get; init; }
    public string? PaymentReference { get; init; }
    public bool PaymentReferenceValidated { get; init; }
    public string? PaymentValidationMessageAr { get; init; }
    public bool HasIdentityDocument { get; init; }
    public TelecomDocumentStatus DocumentStatus { get; init; }
    public bool CanApprove { get; init; }
    public string? BlockReasonAr { get; init; }
    public DateTime? CreatedAtUtc { get; init; }

    // GLOBAL HARDENING: BDR Financial Ledger
    public decimal? OutstandingBalanceSnapshot { get; init; }
    public decimal? WriteOffAmount { get; init; }
    public decimal? CollectedAmount { get; init; }
    public string? CollectionAction { get; init; }
    public string? DunningStage { get; init; }
    public string? AccountType { get; init; }

    // GLOBAL HARDENING: SLA & Queue Management
    public DateTime? SlaExpirationTimeUtc { get; set; }
    public string? ClaimedByUserId { get; set; }
    public string? UpdatedById { get; init; }
    public string? AuditorDisplayName { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public bool SlaBreached { get; init; }
}

public sealed class GetPendingBackOfficeOperationsResult
{
    public IReadOnlyList<PendingBackOfficeOperationDto> Data { get; init; } = [];
    public int Total { get; init; }
}

public sealed class GetPendingBackOfficeOperationsRequest : IRequest<GetPendingBackOfficeOperationsResult>, IRequirePermission
{
    public TelecomOperationKind? Kind { get; init; }
    public BackOfficeDomain? Domain { get; init; }
    public bool IncludeResolved { get; init; }
    public string? SearchTerm { get; init; }
    public bool? SlaBreached { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;

    public string PermissionKey => PermissionCatalog.FinanceBdrView;
}

public sealed class GetPendingBackOfficeOperationsHandler
    : IRequestHandler<GetPendingBackOfficeOperationsRequest, GetPendingBackOfficeOperationsResult>
{
    private readonly IQueryContext _query;
    private readonly IBackOfficePaymentReferenceValidator _paymentValidator;

    public GetPendingBackOfficeOperationsHandler(
        IQueryContext query,
        IBackOfficePaymentReferenceValidator paymentValidator)
    {
        _query = query;
        _paymentValidator = paymentValidator;
    }

    public async Task<GetPendingBackOfficeOperationsResult> Handle(
        GetPendingBackOfficeOperationsRequest request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 200);
        var skip = Math.Max(0, request.Skip);

        var baseQuery = _query.TelecomOperationRequest.AsNoTracking()
            .Include(o => o.SubscriberProfile!)
                .ThenInclude(p => p!.Customer)
            .Include(o => o.MsisdnAsset)
            .Where(o => !o.IsDeleted && o.ApprovalLevelRequired == "BackOffice");

        if (!request.IncludeResolved)
        {
            baseQuery = baseQuery.Where(o => 
                o.Status == TelecomOperationStatus.PendingDocuments 
                || o.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance
                || o.Status == TelecomOperationStatus.In_Progress);
        }
        else
        {
            // For historical ledger, we might want to show everything that was once in the BO queue
            baseQuery = baseQuery.Where(o => 
                o.Status == TelecomOperationStatus.Completed 
                || o.Status == TelecomOperationStatus.Failed 
                || o.Status == TelecomOperationStatus.Approved_Pending_Cash
                || o.Status == TelecomOperationStatus.PendingDocuments 
                || o.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance
                || o.Status == TelecomOperationStatus.In_Progress);
        }

        if (request.Kind.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.Kind == request.Kind.Value);
        }

        if (request.Domain.HasValue)
        {
            var kinds = GetKindsForDomain(request.Domain.Value);
            baseQuery = baseQuery.Where(o => kinds.Contains(o.Kind));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            baseQuery = baseQuery.Where(o => 
                o.Number.Contains(term) 
                || (o.MsisdnAsset != null && o.MsisdnAsset.Msisdn.Contains(term))
                || (o.SubscriberProfile != null && o.SubscriberProfile.Customer != null && o.SubscriberProfile.Customer.DisplayName.Contains(term)));
        }

        if (request.SlaBreached.HasValue)
        {
            var now = DateTime.UtcNow;
            if (request.SlaBreached.Value)
            {
                baseQuery = baseQuery.Where(o => o.SlaExpirationTimeUtc.HasValue && o.SlaExpirationTimeUtc.Value < now);
            }
            else
            {
                baseQuery = baseQuery.Where(o => !o.SlaExpirationTimeUtc.HasValue || o.SlaExpirationTimeUtc.Value >= now);
            }
        }

        if (request.FromUtc.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            baseQuery = baseQuery.Where(o => o.CreatedAtUtc <= request.ToUtc.Value);
        }

        var total = await baseQuery.CountAsync(cancellationToken);
        var rows = await baseQuery
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var data = new List<PendingBackOfficeOperationDto>(rows.Count);
        foreach (var op in rows)
        {
            data.Add(await MapAsync(op, cancellationToken));
        }

        return new GetPendingBackOfficeOperationsResult { Data = data, Total = total };
    }

    private static List<TelecomOperationKind> GetKindsForDomain(BackOfficeDomain domain)
    {
        return domain switch
        {
            BackOfficeDomain.FinanceAndRevenue =>
            [
                TelecomOperationKind.BadDebtRecovery,
                TelecomOperationKind.DepositRefundSettlement,
                TelecomOperationKind.Reconnect
            ],
            BackOfficeDomain.NetworkAndTechnical =>
            [
                TelecomOperationKind.TemporarySuspension,
                TelecomOperationKind.ChangeGsmType,
                TelecomOperationKind.ServiceModification
            ],
            BackOfficeDomain.CoreAdministrative =>
            [
                TelecomOperationKind.NewActivation,
                TelecomOperationKind.Migration,
                TelecomOperationKind.TakeOver,
                TelecomOperationKind.SimSwap,
                TelecomOperationKind.Termination,
                TelecomOperationKind.NumberPortability,
                TelecomOperationKind.DeviceSale
            ],
            _ => []
        };
    }

    private async Task<PendingBackOfficeOperationDto> MapAsync(
        TelecomOperationRequest op,
        CancellationToken cancellationToken)
    {
        var paymentValidation = op.Kind == TelecomOperationKind.Reconnect
            ? await _paymentValidator.ValidateReconnectPaymentAsync(op, cancellationToken)
            : new BackOfficePaymentValidationResult(true, "—");

        var docsReady = op.DocumentStatus >= TelecomDocumentStatus.Uploaded
                        || !string.IsNullOrEmpty(op.IdentityDocumentStorageKey);

        string? blockReason = null;
        if (!docsReady)
        {
            blockReason = "يجب رفع وثيقة الهوية قبل الاعتماد.";
        }
        else if (!paymentValidation.IsValid)
        {
            blockReason = paymentValidation.MessageAr;
        }

        var canApprove = docsReady && paymentValidation.IsValid;

        var slaBreached = op.SlaExpirationTimeUtc.HasValue && op.SlaExpirationTimeUtc.Value < DateTime.UtcNow;

        var accountType = op.SubscriberProfile?.Customer?.CustomerKind == CustomerKind.Corporate
            ? "Postpaid Corporate Account"
            : "Postpaid Individual Account";

        return new PendingBackOfficeOperationDto
        {
            Id = op.Id,
            Number = op.Number,
            Kind = op.Kind,
            KindNameAr = TelecomOperationLabels.KindLabelAr(op.Kind),
            PipelineState = BackOfficeTelecomPipelineState.Resolve(op.Status, op.ApprovalLevelRequired),
            Msisdn = op.MsisdnAsset?.Msisdn,
            SubscriberName = op.SubscriberProfile?.Customer?.DisplayName,
            SuspensionType = op.SuspensionType,
            ClearanceType = op.ClearanceType,
            PaymentReference = op.PaymentReference,
            PaymentReferenceValidated = paymentValidation.IsValid,
            PaymentValidationMessageAr = paymentValidation.MessageAr,
            HasIdentityDocument = !string.IsNullOrEmpty(op.IdentityDocumentStorageKey),
            DocumentStatus = op.DocumentStatus,
            CanApprove = canApprove,
            BlockReasonAr = blockReason,
            CreatedAtUtc = op.CreatedAtUtc,
            OutstandingBalanceSnapshot = op.OutstandingBalanceSnapshot,
            WriteOffAmount = op.WriteOffAmount,
            CollectedAmount = op.CollectedAmount,
            CollectionAction = op.CollectionAction,
            DunningStage = op.DunningStage,
            AccountType = accountType,
            SlaExpirationTimeUtc = op.SlaExpirationTimeUtc,
            ClaimedByUserId = op.ClaimedByUserId,
            UpdatedById = op.UpdatedById,
            AuditorDisplayName = null,
            UpdatedAtUtc = op.UpdatedAtUtc,
            SlaBreached = slaBreached
        };
    }
}
