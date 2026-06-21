using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.BackOffice;
using Application.Common.Telecom.Confirm;
using Application.Common.Telecom.SellingLine;
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
    public string? MessageAr { get; init; }
}

public sealed class ApproveBackOfficeTelecomOperationRequest : IRequest<ApproveBackOfficeTelecomOperationResult>, IRequireAnyPermission
{
    public string OperationId { get; init; } = null!;
    public string? Comments { get; init; }

    public IReadOnlyList<string> PermissionKeys => BackOfficePermissionSets.BdrExecuteAny;
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
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAuditService _audit;
    private readonly IBackOfficePaymentReferenceValidator _paymentValidator;
    private readonly IOperatorContext _operatorContext;
    private readonly ITelecomActivationWorkflow _workflow;
    private readonly IConfirmTelecomOperationPermissionGate _confirmPermissionGate;
    private readonly ISellingLineEligibilityChecker _sellingLineEligibility;
    private readonly IBackOfficeRcnBdrSettlementService _rcnBdrSettlement;

    public ApproveBackOfficeTelecomOperationHandler(
        IQueryContext query,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IUnitOfWork unitOfWork,
        IUserAuditService audit,
        IBackOfficePaymentReferenceValidator paymentValidator,
        IOperatorContext operatorContext,
        ITelecomActivationWorkflow workflow,
        IConfirmTelecomOperationPermissionGate confirmPermissionGate,
        ISellingLineEligibilityChecker sellingLineEligibility,
        IBackOfficeRcnBdrSettlementService rcnBdrSettlement)
    {
        _query = query;
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _paymentValidator = paymentValidator;
        _operatorContext = operatorContext;
        _workflow = workflow;
        _confirmPermissionGate = confirmPermissionGate;
        _sellingLineEligibility = sellingLineEligibility;
        _rcnBdrSettlement = rcnBdrSettlement;
    }

    public async Task<ApproveBackOfficeTelecomOperationResult> Handle(
        ApproveBackOfficeTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operatorContext);

        var operation = await _query.TelecomOperationRequest.AsNoTracking()
            .Include(o => o.MsisdnAsset)
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == request.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        // Multi-Tenancy / RLS Check — national demo anchors stay visible across branches.
        var msisdn = operation.MsisdnAsset?.Msisdn;
        var isNationalAnchor = !string.IsNullOrWhiteSpace(msisdn) && TelecomDemoMsisdn.IsWellKnown(msisdn.Trim());
        if (!isNationalAnchor
            && !string.IsNullOrEmpty(operation.BranchId)
            && !string.IsNullOrEmpty(_operatorContext.BranchId)
            && operation.BranchId != _operatorContext.BranchId)
        {
            throw new UnauthorizedPermissionException("Security Violation: Regional boundary mismatch. You are not authorized to approve operations from a different branch.");
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

            await _rcnBdrSettlement.EnsureAutoSettlementForPaidReconnectAsync(
                operation,
                actorUserId,
                cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        if (operation.Kind == TelecomOperationKind.BadDebtRecovery)
        {
            // GLOBAL HARDENING: BDR transition to Approved_Pending_Cash
            var tracked = await _operationRepository.GetAsync(request.OperationId, cancellationToken)
                ?? throw new InvalidOperationException("Telecom operation not found.");

            tracked.Status = TelecomOperationStatus.Approved_Pending_Cash;
            tracked.FraudClearanceConfirmed = true;
            tracked.FraudClearanceByUserId = actorUserId;
            tracked.ConfirmedAtUtc = DateTime.UtcNow;
            tracked.UpdatedById = actorUserId;

            _operationRepository.Update(tracked);
            await _unitOfWork.SaveAsync(cancellationToken);

            var msisdnBdr = operation.MsisdnAsset?.Msisdn ?? "—";
            await _audit.LogAsync(
                new UserAuditLogRequest
                {
                    ActorUserId = actorUserId,
                    ActionType = UserAuditActionTypes.BackOfficeTelecomApproved,
                    EntityType = nameof(TelecomOperationRequest),
                    EntityId = request.OperationId,
                    SummaryAr = $"اعتماد باك أوفيس (BDR) — بانتظار التحصيل المالي: {msisdnBdr}",
                    Payload = new { request.OperationId, operation.Number, operation.Kind, msisdn = msisdnBdr, status = "Approved_Pending_Cash" }
                }, cancellationToken);

            const string bdrMessage = "تم اعتماد طلب تسوية الديون. بانتظار التحصيل المالي في المعرض.";
            return new ApproveBackOfficeTelecomOperationResult
            {
                ConfirmResult = new ConfirmTelecomOperationRequestResult { Data = tracked, UserMessageAr = bdrMessage },
                PipelineState = BackOfficeTelecomPipelineState.ApprovedPendingCash,
                MessageAr = bdrMessage,
            };
        }

        var confirm = await ExecuteBackOfficeConfirmAsync(operation, request.OperationId, actorUserId, cancellationToken);

        var msisdnLabel = operation.MsisdnAsset?.Msisdn ?? "—";
        var summary = BuildApprovalSummary(operation, msisdnLabel, request.Comments);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId,
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
                    msisdn = msisdnLabel,
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
            MessageAr = confirm.UserMessageAr ?? confirm.StatusHintAr,
        };
    }

    private async Task<ConfirmTelecomOperationRequestResult> ExecuteBackOfficeConfirmAsync(
        TelecomOperationRequest operation,
        string operationId,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        await _confirmPermissionGate.EnsureCanConfirmAsync(operation, cancellationToken);
        await _sellingLineEligibility.ValidateForConfirmAsync(operation, cancellationToken);

        var workflowResult = await _workflow.ConfirmActivationAsync(operationId, actorUserId, cancellationToken);
        EnsureBackOfficeApprovalPersisted(workflowResult);

        var status = workflowResult.Operation?.Status ?? TelecomOperationStatus.Failed;
        var (hintAr, hintEn) = ConfirmTelecomOperationStatusHints.Map(workflowResult, status);

        return new ConfirmTelecomOperationRequestResult
        {
            Data = workflowResult.Operation,
            BillingResult = workflowResult.BillingResult,
            NetworkResult = workflowResult.NetworkResult,
            IdempotentReplay = workflowResult.IdempotentReplay,
            StatusHintAr = hintAr,
            StatusHintEn = hintEn,
            UserMessageAr = workflowResult.MessageAr ?? workflowResult.Message ?? hintAr,
            UserMessageEn = workflowResult.MessageEn ?? workflowResult.Message ?? hintEn,
            HlrCompletesAsynchronously = status is TelecomOperationStatus.Provisioning
                or TelecomOperationStatus.PendingExternal,
        };
    }

    private static void EnsureBackOfficeApprovalPersisted(TelecomActivationWorkflowResult result)
    {
        if (result.BillingResult is { Success: false })
        {
            throw new BusinessRuleViolationException(
                result.MessageAr ?? result.Message ?? "فشل اعتماد الطلب — لم يكتمل التزامن مع CBS/HLR.");
        }

        if (result.Operation?.Status is not { } status
            || BackOfficeTelecomPipelineState.IsActiveBackOfficeQueueStatus(status))
        {
            throw new BusinessRuleViolationException(
                result.MessageAr ?? result.Message ?? "فشل اعتماد الطلب — الحالة لم تتغير.");
        }
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
