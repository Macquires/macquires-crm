using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Telecom;
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

    public bool HlrCompletesAsynchronously { get; set; }
}

public class ConfirmTelecomOperationRequest : IRequest<ConfirmTelecomOperationRequestResult>
{
    public string Id { get; init; } = null!;
    public string? UpdatedById { get; init; }
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

    public ConfirmTelecomOperationRequestHandler(
        ITelecomActivationWorkflow workflow,
        IUserAuditService audit,
        IQueryContext query,
        IOperatorContext operatorContext,
        IPermissionEvaluator permissions)
    {
        _workflow = workflow;
        _audit = audit;
        _query = query;
        _operator = operatorContext;
        _permissions = permissions;
    }

    public async Task<ConfirmTelecomOperationRequestResult> Handle(
        ConfirmTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var operation = await _query.TelecomOperationRequest.AsNoTracking()
            .Include(o => o.MsisdnAsset)
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        await EnsurePermissionForKindAsync(operation.Kind, cancellationToken);

        var result = await _workflow.ConfirmActivationAsync(request.Id, request.UpdatedById, cancellationToken);

        var msisdn = operation.MsisdnAsset?.Msisdn ?? "—";
        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.UpdatedById ?? "system",
                ActionType = UserAuditActionTypes.TelecomOperationConfirmed,
                EntityType = nameof(TelecomOperationRequest),
                EntityId = request.Id,
                SummaryAr = operation.Kind == TelecomOperationKind.TakeOver
                    ? $"اعتماد نقل ملكية {msisdn} — CBS/HLR"
                    : $"تأكيد {operation.Kind} للخط {msisdn}",
                Payload = new { request.Id, operation.Kind, msisdn, result.IdempotentReplay, source = "Customer360" },
            },
            cancellationToken);

        var op = result.Operation;
        var status = op?.Status ?? TelecomOperationStatus.Failed;
        var hint = status switch
        {
            TelecomOperationStatus.Completed =>
                "تم تسجيل العملية واكتمل التزامن مع CBS وHLR.",
            TelecomOperationStatus.PendingExternal =>
                "تم تسجيل العملية؛ المزامنة مع CBS/HLR قيد الانتظار (شبكة أو إعدادات تكامل).",
            TelecomOperationStatus.Provisioning =>
                "تم تأكيد CBS؛ جاري تزويد الشبكة (HLR) في الخلفية.",
            TelecomOperationStatus.Failed =>
                "فشلت العملية — راجع سجل التكامل أو أعد المحاولة من الباك أوفيس.",
            _ => "تم استلام الطلب."
        };

        return new ConfirmTelecomOperationRequestResult
        {
            Data = op,
            BillingResult = result.BillingResult,
            NetworkResult = result.NetworkResult,
            IdempotentReplay = result.IdempotentReplay,
            StatusHintAr = hint,
            HlrCompletesAsynchronously = status is TelecomOperationStatus.Provisioning
                or TelecomOperationStatus.PendingExternal
        };
    }

    private async Task EnsurePermissionForKindAsync(TelecomOperationKind kind, CancellationToken cancellationToken)
    {
        if (!_operator.IsAuthenticated || string.IsNullOrEmpty(_operator.UserId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ هذه العملية.");
        }

        var key = TelecomOperationPermissionResolver.PermissionKeyForKind(kind);
        if (!await _permissions.HasPermissionAsync(_operator.UserId, key, cancellationToken))
        {
            throw new BusinessRuleViolationException("ليس لديك صلاحية لتنفيذ هذه العملية.");
        }
    }
}
