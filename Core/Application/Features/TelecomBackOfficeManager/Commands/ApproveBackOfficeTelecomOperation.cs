using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom.BackOffice;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public sealed class ApproveBackOfficeTelecomOperationResult
{
    public ConfirmTelecomOperationRequestResult? ConfirmResult { get; init; }
    public string? PipelineState { get; init; }
}

public sealed class ApproveBackOfficeTelecomOperationRequest : IRequest<ApproveBackOfficeTelecomOperationResult>, IRequirePermission
{
    public string OperationId { get; init; } = null!;
    public string? ActorUserId { get; init; }
    public string? Comments { get; init; }

    public string PermissionKey => PermissionCatalog.FinanceBdrExecute;
}

public sealed class ApproveBackOfficeTelecomOperationValidator : AbstractValidator<ApproveBackOfficeTelecomOperationRequest>
{
    public ApproveBackOfficeTelecomOperationValidator()
    {
        RuleFor(x => x.OperationId).NotEmpty();
    }
}

public sealed class ApproveBackOfficeTelecomOperationHandler
    : IRequestHandler<ApproveBackOfficeTelecomOperationRequest, ApproveBackOfficeTelecomOperationResult>
{
    private readonly IMediator _mediator;
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAuditService _audit;
    private readonly IBackOfficePaymentReferenceValidator _paymentValidator;
    private readonly IOperatorContext _operatorContext;

    public ApproveBackOfficeTelecomOperationHandler(
        IMediator mediator,
        IQueryContext query,
        IUnitOfWork unitOfWork,
        IUserAuditService audit,
        IBackOfficePaymentReferenceValidator paymentValidator,
        IOperatorContext operatorContext)
    {
        _mediator = mediator;
        _query = query;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _paymentValidator = paymentValidator;
        _operatorContext = operatorContext;
    }

    public async Task<ApproveBackOfficeTelecomOperationResult> Handle(
        ApproveBackOfficeTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var operation = await _query.TelecomOperationRequest.AsNoTracking()
            .Include(o => o.MsisdnAsset)
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == request.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        // Multi-Tenancy / RLS Check
        if (!string.IsNullOrEmpty(operation.BranchId) && !string.IsNullOrEmpty(_operatorContext.BranchId))
        {
            if (operation.BranchId != _operatorContext.BranchId)
            {
                throw new UnauthorizedPermissionException("Security Violation: Regional boundary mismatch. You are not authorized to approve operations from a different branch.");
            }
        }

        if (!BackOfficeTelecomPipelineState.IsBackOfficeQueueCandidate(operation))
        {
            throw new BusinessRuleViolationException(
                "الطلب ليس في طابور اعتماد الباك أوفيس (Pending_BackOffice_Approval).");
        }

        var docsReady = operation.DocumentStatus >= TelecomDocumentStatus.Uploaded
                        || !string.IsNullOrEmpty(operation.IdentityDocumentStorageKey);
        if (!docsReady)
        {
            throw new BusinessRuleViolationException("يجب رفع وثيقة الهوية قبل اعتماد الطلب.");
        }

        if (operation.Kind == TelecomOperationKind.Reconnect)
        {
            var paymentCheck = await _paymentValidator.ValidateReconnectPaymentAsync(operation, cancellationToken);
            if (!paymentCheck.IsValid)
            {
                throw new BusinessRuleViolationException(paymentCheck.MessageAr);
            }
        }

        if (operation.Kind == TelecomOperationKind.BadDebtRecovery)
        {
            // GLOBAL HARDENING: BDR transition to Approved_Pending_Cash
            var tracked = await _query.TelecomOperationRequest.FirstOrDefaultAsync(o => o.Id == request.OperationId, cancellationToken)
                ?? throw new InvalidOperationException("Telecom operation not found.");
            
            tracked.Status = TelecomOperationStatus.Approved_Pending_Cash;
            tracked.FraudClearanceConfirmed = true;
            tracked.FraudClearanceByUserId = request.ActorUserId;
            tracked.ConfirmedAtUtc = DateTime.UtcNow;
            tracked.UpdatedById = request.ActorUserId;
            
            // We don't call ConfirmTelecomOperationRequest for BDR yet, as it needs cash collection first
            await _unitOfWork.SaveAsync(cancellationToken);

            var msisdnBdr = operation.MsisdnAsset?.Msisdn ?? "—";
            await _audit.LogAsync(
                new UserAuditLogRequest
                {
                    ActorUserId = request.ActorUserId ?? "system",
                    ActionType = UserAuditActionTypes.BackOfficeTelecomApproved,
                    EntityType = nameof(TelecomOperationRequest),
                    EntityId = request.OperationId,
                    SummaryAr = $"اعتماد باك أوفيس (BDR) — بانتظار التحصيل المالي: {msisdnBdr}",
                    Payload = new { request.OperationId, operation.Number, operation.Kind, msisdn = msisdnBdr, status = "Approved_Pending_Cash" }
                }, cancellationToken);

            return new ApproveBackOfficeTelecomOperationResult
            {
                ConfirmResult = new ConfirmTelecomOperationRequestResult { Data = tracked, UserMessageAr = "تم اعتماد طلب تسوية الديون. بانتظار التحصيل المالي في المعرض." },
                PipelineState = "Approved_Pending_Cash"
            };
        }

        // GLOBAL HARDENING: Route B Bypass to Advance - Back-Office Audit
        if (operation.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance)
        {
            var confirmBypass = await _mediator.Send(
                new ConfirmTelecomOperationRequest
                {
                    Id = request.OperationId,
                    UpdatedById = request.ActorUserId,
                },
                cancellationToken);

            return new ApproveBackOfficeTelecomOperationResult
            {
                ConfirmResult = confirmBypass,
                PipelineState = "Completed"
            };
        }

        var confirm = await _mediator.Send(
            new ConfirmTelecomOperationRequest
            {
                Id = request.OperationId,
                UpdatedById = request.ActorUserId,
            },
            cancellationToken);

        var msisdn = operation.MsisdnAsset?.Msisdn ?? "—";
        var summary = BuildApprovalSummary(operation, msisdn, request.Comments);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.ActorUserId ?? "system",
                ActionType = UserAuditActionTypes.BackOfficeTelecomApproved,
                EntityType = nameof(TelecomOperationRequest),
                EntityId = request.OperationId,
                SummaryAr = summary,
                Payload = new
                {
                    request.OperationId,
                    operation.Number,
                    operation.Kind,
                    operation.SuspensionType,
                    operation.ClearanceType,
                    msisdn,
                    request.Comments,
                    confirm.HlrCompletesAsynchronously,
                },
            },
            cancellationToken);

        var pipelineState = confirm.Data?.Status is { } status
            ? BackOfficeTelecomPipelineState.Resolve(status, operation.ApprovalLevelRequired)
            : BackOfficeTelecomPipelineState.Executing;

        return new ApproveBackOfficeTelecomOperationResult
        {
            ConfirmResult = confirm,
            PipelineState = pipelineState,
        };
    }

    private static string BuildApprovalSummary(
        TelecomOperationRequest operation,
        string msisdn,
        string? comments)
    {
        var typeHint = operation.Kind switch
        {
            TelecomOperationKind.TemporarySuspension => $"Fraud/Regulatory Clearance SUS ({operation.SuspensionType})",
            TelecomOperationKind.Reconnect => $"Reconnect Clearance RCN ({operation.ClearanceType})",
            _ => operation.Kind.ToString(),
        };

        var baseSummary = $"اعتماد باك أوفيس — {typeHint} للطلب {operation.Number} / {msisdn}";
        return string.IsNullOrWhiteSpace(comments) ? baseSummary : $"{baseSummary} — {comments.Trim()}";
    }
}
