using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.SellingLine;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class ConfirmTelecomOperationRequestResult
{
    public TelecomOperationRequest? Data { get; set; }
    public BillingProvisionResult? BillingResult { get; set; }
    public NetworkProvisionResult? NetworkResult { get; set; }
    public bool IdempotentReplay { get; set; }

    /// <summary>Arabic hint for UI (CBS done; HLR may complete asynchronously).</summary>
    public string? StatusHintAr { get; set; }

    /// <summary>English hint for UI.</summary>
    public string? StatusHintEn { get; set; }

    public string? UserMessageAr { get; set; }
    public string? UserMessageEn { get; set; }

    public bool HlrCompletesAsynchronously { get; set; }
}

public class ConfirmTelecomOperationRequest : IRequest<ConfirmTelecomOperationRequestResult>
{
    public string Id { get; init; } = null!;
    public string? UpdatedById { get; init; }

    /// <summary>BackOffice override for VAL-02-01 KYC gate (Selling Line only).</summary>
    public string? OverrideReasonCode { get; init; }
}

public class ConfirmTelecomOperationRequestValidator : AbstractValidator<ConfirmTelecomOperationRequest>
{
    public ConfirmTelecomOperationRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ConfirmTelecomOperationRequestHandler : IRequestHandler<ConfirmTelecomOperationRequest, ConfirmTelecomOperationRequestResult>
{
    private readonly ITelecomActivationWorkflow _workflow;
    private readonly IUserAuditService _audit;
    private readonly IQueryContext _query;
    private readonly IOperatorContext _operator;
    private readonly IPermissionEvaluator _permissions;
    private readonly ISellingLineEligibilityChecker _sellingLineEligibility;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmTelecomOperationRequestHandler(
        ITelecomActivationWorkflow workflow,
        IUserAuditService audit,
        IQueryContext query,
        IOperatorContext operatorContext,
        IPermissionEvaluator permissions,
        ISellingLineEligibilityChecker sellingLineEligibility,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IUnitOfWork unitOfWork)
    {
        _workflow = workflow;
        _audit = audit;
        _query = query;
        _operator = operatorContext;
        _permissions = permissions;
        _sellingLineEligibility = sellingLineEligibility;
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConfirmTelecomOperationRequestResult> Handle(
        ConfirmTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var operation = await _query.TelecomOperationRequest.AsNoTracking()
            .Include(o => o.MsisdnAsset)
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        await EnsurePermissionAsync(operation, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.OverrideReasonCode))
        {
            await ApplySellingLineOverrideAsync(operation, request, cancellationToken);
            operation = await _query.TelecomOperationRequest.AsNoTracking()
                .Include(o => o.MsisdnAsset)
                .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == request.Id, cancellationToken)
                ?? throw new InvalidOperationException("Telecom operation not found.");
        }

        await _sellingLineEligibility.ValidateForConfirmAsync(operation, cancellationToken);

        var result = await _workflow.ConfirmActivationAsync(request.Id, request.UpdatedById, cancellationToken);

        var msisdn = operation.MsisdnAsset?.Msisdn ?? "—";
        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.UpdatedById ?? "system",
                ActionType = UserAuditActionTypes.TelecomOperationConfirmed,
                EntityType = nameof(TelecomOperationRequest),
                EntityId = request.Id,
                SummaryAr = operation.Kind switch
                {
                    TelecomOperationKind.TakeOver => $"اعتماد نقل ملكية {msisdn} — CBS/HLR",
                    TelecomOperationKind.SimSwap => $"اعتماد تبديل شريحة {msisdn} — CBS/HLR",
                    TelecomOperationKind.ChangeGsmType => $"تأكيد تحويل نوع الخط CGT {msisdn} — CBS/HLR",
                    TelecomOperationKind.NumberPortability => $"اعتماد تغيير رقم CNR {msisdn} — CBS/HLR",
                    TelecomOperationKind.Termination => $"اعتماد إنهاء خط TRM {msisdn} — CBS/HLR",
                    TelecomOperationKind.TemporarySuspension => $"اعتماد حظر خط SUS {msisdn} — CBS/HLR",
                    TelecomOperationKind.Reconnect => $"اعتماد إعادة تفعيل RCN {msisdn} — CBS/HLR",
                    _ => $"تأكيد {operation.Kind} للخط {msisdn}",
                },
                Payload = new { request.Id, operation.Kind, msisdn, result.IdempotentReplay, source = "Customer360" },
            },
            cancellationToken);

        var op = result.Operation;
        var status = op?.Status ?? TelecomOperationStatus.Failed;
        var (hintAr, hintEn) = status switch
        {
            TelecomOperationStatus.Completed => (
                "تم تسجيل العملية واكتمل التزامن مع CBS وHLR.",
                "Operation recorded; CBS and HLR sync completed."),
            TelecomOperationStatus.PendingExternal => (
                "تم تسجيل العملية؛ المزامنة مع CBS/HLR قيد الانتظار (شبكة أو إعدادات تكامل).",
                "Operation recorded; CBS/HLR sync is pending (network or integration settings)."),
            TelecomOperationStatus.Provisioning => (
                "تم تأكيد CBS؛ جاري تزويد الشبكة (HLR) في الخلفية.",
                "CBS confirmed; network provisioning (HLR) is running in the background."),
            TelecomOperationStatus.Failed => (
                result.MessageAr ?? result.Message,
                result.MessageEn ?? result.Message),
            _ => ("تم استلام الطلب.", "Request received.")
        };

        if (status == TelecomOperationStatus.Failed
            && string.IsNullOrWhiteSpace(result.MessageAr)
            && string.IsNullOrWhiteSpace(result.MessageEn))
        {
            hintAr = "فشلت العملية — راجع سجل التكامل أو أعد المحاولة من الباك أوفيس.";
            hintEn = "Operation failed — review the integration log or retry from back office.";
        }

        return new ConfirmTelecomOperationRequestResult
        {
            Data = op,
            BillingResult = result.BillingResult,
            NetworkResult = result.NetworkResult,
            IdempotentReplay = result.IdempotentReplay,
            StatusHintAr = hintAr,
            StatusHintEn = hintEn,
            UserMessageAr = result.MessageAr ?? result.Message,
            UserMessageEn = result.MessageEn ?? result.Message,
            HlrCompletesAsynchronously = status is TelecomOperationStatus.Provisioning
                or TelecomOperationStatus.PendingExternal
        };
    }

    private async Task EnsurePermissionAsync(TelecomOperationRequest operation, CancellationToken cancellationToken)
    {
        if (!_operator.IsAuthenticated || string.IsNullOrEmpty(_operator.UserId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ هذه العملية.");
        }

        foreach (var key in ResolvePermissionKeys(operation))
        {
            if (await _permissions.HasPermissionAsync(_operator.UserId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException("ليس لديك صلاحية لتنفيذ هذه العملية.");
    }

    private static IEnumerable<string> ResolvePermissionKeys(TelecomOperationRequest operation)
    {
        if (operation.Kind == TelecomOperationKind.SimSwap && operation.IsLostOrStolenReport)
        {
            yield return PermissionCatalog.TelecomLineSimSwapApprove;
            yield return PermissionCatalog.TelecomLineSimSwap;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.SimSwap)
        {
            yield return PermissionCatalog.TelecomLineSimSwap;
            yield return PermissionCatalog.TelecomLineSimSwapApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.NumberPortability
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineChangeNumberApprove;
            yield return PermissionCatalog.TelecomLineChangeNumber;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.NumberPortability)
        {
            yield return PermissionCatalog.TelecomLineChangeNumber;
            yield return PermissionCatalog.TelecomLineChangeNumberApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.Termination
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineTerminationApprove;
            yield return PermissionCatalog.TelecomLineTermination;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.Termination)
        {
            yield return PermissionCatalog.TelecomLineTermination;
            yield return PermissionCatalog.TelecomLineTerminationApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.TemporarySuspension
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineSuspensionApprove;
            yield return PermissionCatalog.TelecomLineSuspension;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.TemporarySuspension)
        {
            yield return PermissionCatalog.TelecomLineSuspension;
            yield return PermissionCatalog.TelecomLineSuspensionApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.Reconnect
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineReconnectApprove;
            yield return PermissionCatalog.TelecomLineReconnect;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.Reconnect)
        {
            yield return PermissionCatalog.TelecomLineReconnect;
            yield return PermissionCatalog.TelecomLineReconnectApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.DepositRefundSettlement
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineRefundApprove;
            yield return PermissionCatalog.TelecomLineRefund;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.DepositRefundSettlement)
        {
            yield return PermissionCatalog.TelecomLineRefund;
            yield return PermissionCatalog.TelecomLineRefundApprove;
            yield break;
        }

        yield return TelecomOperationPermissionResolver.PermissionKeyForKind(operation.Kind);
    }

    private async Task ApplySellingLineOverrideAsync(
        TelecomOperationRequest operation,
        ConfirmTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (operation.Kind != TelecomOperationKind.NewActivation)
        {
            throw new BusinessRuleViolationException("سبب الاستثناء متاح لعمليات تفعيل خط جديد فقط.");
        }

        var canOverride = _operator.Roles.Any(r =>
            r.Equals("TelecomBackOffice", StringComparison.OrdinalIgnoreCase)
            || r.Equals("TelecomAdmin", StringComparison.OrdinalIgnoreCase));
        if (!canOverride)
        {
            throw new BusinessRuleViolationException("تسجيل سبب الاستثناء متاح لموظفي الباك أوفيس فقط.");
        }

        var tracked = await _operationRepository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        SellingLineOperationMutationGuard.EnsureEditableInventoryFields(
            tracked, tracked.MsisdnAssetId, tracked.SimInventoryId, tracked.ProductOfferingId, tracked.ProductId);

        tracked.OverrideReasonCode = request.OverrideReasonCode!.Trim();
        tracked.UpdatedById = request.UpdatedById;
        _operationRepository.Update(tracked);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
