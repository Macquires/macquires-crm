using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Common.Telecom.BackOffice;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public sealed class RejectBackOfficeTelecomOperationResult
{
    public TelecomOperationRequest? Data { get; init; }
    public string PipelineState { get; init; } = BackOfficeTelecomPipelineState.Failed;
    public string? MessageAr { get; init; }
}

public sealed class RejectBackOfficeTelecomOperationRequest : IRequest<RejectBackOfficeTelecomOperationResult>, IRequireAnyPermission
{
    public string OperationId { get; init; } = null!;
    public string RejectionReason { get; init; } = null!;

    public IReadOnlyList<string> PermissionKeys => BackOfficePermissionSets.BdrExecuteAny;
}

public sealed class RejectBackOfficeTelecomOperationValidator : AbstractValidator<RejectBackOfficeTelecomOperationRequest>
{
    public RejectBackOfficeTelecomOperationValidator()
    {
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.RejectionReason).NotEmpty().MinimumLength(5).MaximumLength(500);
    }
}

public sealed class RejectBackOfficeTelecomOperationHandler
    : IRequestHandler<RejectBackOfficeTelecomOperationRequest, RejectBackOfficeTelecomOperationResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly ITelecomOperationOrchestrator _orchestrator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _query;
    private readonly IUserAuditService _audit;
    private readonly IBillingSystemIntegration _billing;
    private readonly IHLRLiveStatusService _hlr;
    private readonly IOperatorContext _operatorContext;

    public RejectBackOfficeTelecomOperationHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        ITelecomOperationOrchestrator orchestrator,
        IUnitOfWork unitOfWork,
        IQueryContext query,
        IUserAuditService audit,
        IBillingSystemIntegration billing,
        IHLRLiveStatusService hlr,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _orchestrator = orchestrator;
        _unitOfWork = unitOfWork;
        _query = query;
        _audit = audit;
        _billing = billing;
        _hlr = hlr;
        _operatorContext = operatorContext;
    }

    public async Task<RejectBackOfficeTelecomOperationResult> Handle(
        RejectBackOfficeTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operatorContext);

        var operation = await _repository.GetAsync(request.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        // Multi-Tenancy / RLS Check
        if (!string.IsNullOrEmpty(operation.BranchId) && !string.IsNullOrEmpty(_operatorContext.BranchId))
        {
            if (operation.BranchId != _operatorContext.BranchId)
            {
                throw new UnauthorizedPermissionException("Security Violation: Regional boundary mismatch. You are not authorized to reject operations from a different branch.");
            }
        }

        if (!BackOfficeTelecomPipelineState.IsBackOfficeQueueCandidate(operation))
        {
            throw new BusinessRuleViolationException("الطلب ليس في طابور اعتماد الباك أوفيس.");
        }

        var msisdn = await _query.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId)
            .Select(m => m.Msisdn)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        operation.DocumentStatus = TelecomDocumentStatus.Rejected;
        operation.Notes = AppendNote(operation.Notes, $"{BackOfficeTelecomPipelineState.BackOfficeRejectedNotePrefix} {request.RejectionReason.Trim()}");
        operation.UpdatedById = actorUserId;

        // GLOBAL HARDENING: Route B Bypass to Advance - Back-Office Reject (Critical Revenue Assurance)
        if (operation.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance)
        {
            if (!string.IsNullOrEmpty(msisdn) && msisdn != "—")
            {
                // 1. DO NOT unbar. Instead, KILL leaky HLR status if it was active
                await _hlr.MarkMockSubscriberTerminatedAsync(msisdn, cancellationToken);
                
                // 2. Apply collected cash as partial payment (Unapplied Credit)
                // Outstanding was -15,000. Collected 5,000. New balance = -10,000.
                var currentBalance = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
                var newBalance = currentBalance + (operation.CollectedAmount ?? 5000m);
                
                // MANDATE IDEMPOTENCY: Use OperationId + Status as a composite key for financial mutations
                var idempotencyKey = $"BDR-REJ-{operation.Id}-{operation.Status}";
                await _billing.AdjustBalanceAsync(msisdn, newBalance, "BDR Rejected - Partial Payment Applied", idempotencyKey, cancellationToken);
            }
        }

        await _orchestrator.TransitionAsync(
            operation,
            TelecomOperationStatus.Failed,
            actorUserId,
            $"رفض باك أوفيس: {request.RejectionReason.Trim()}",
            cancellationToken);

        _repository.Update(operation);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId,
                ActionType = UserAuditActionTypes.BackOfficeTelecomRejected,
                EntityType = nameof(TelecomOperationRequest),
                EntityId = request.OperationId,
                SummaryAr = $"رفض باك أوفيس للطلب {operation.Number} ({msisdn}) — {request.RejectionReason.Trim()}",
                Payload = new
                {
                    request.OperationId,
                    operation.Number,
                    operation.Kind,
                    msisdn,
                    request.RejectionReason,
                },
            },
            cancellationToken);

        return new RejectBackOfficeTelecomOperationResult
        {
            Data = operation,
            MessageAr = $"تم رفض الطلب {operation.Number}.",
        };
    }

    private static string AppendNote(string? existing, string line) =>
        string.IsNullOrEmpty(existing) ? line : $"{existing}\n{line}";
}
