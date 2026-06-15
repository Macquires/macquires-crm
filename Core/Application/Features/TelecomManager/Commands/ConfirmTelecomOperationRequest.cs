using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.Confirm;
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

public class ConfirmTelecomOperationRequest : IRequest<ConfirmTelecomOperationRequestResult>, IRequireAnyPermission
{
    public string Id { get; init; } = null!;

    /// <summary>BackOffice override for VAL-02-01 KYC gate (Selling Line only).</summary>
    public string? OverrideReasonCode { get; init; }

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.ConfirmAny;
}

public class ConfirmTelecomOperationRequestValidator : AbstractValidator<ConfirmTelecomOperationRequest>
{
    public ConfirmTelecomOperationRequestValidator(IQueryContext query)
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(64);

        RuleFor(x => x.OverrideReasonCode)
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.OverrideReasonCode));

        RuleFor(x => x)
            .MustAsync(async (request, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.OverrideReasonCode))
                {
                    return true;
                }

                return await query.TelecomOperationRequest.AsNoTracking()
                    .AnyAsync(
                        o => !o.IsDeleted
                             && o.Id == request.Id
                             && o.Kind == TelecomOperationKind.NewActivation,
                        cancellationToken);
            })
            .WithMessage("سبب الاستثناء متاح لعمليات تفعيل خط جديد (ACT) فقط.")
            .When(x => !string.IsNullOrWhiteSpace(x.OverrideReasonCode));
    }
}

public class ConfirmTelecomOperationRequestHandler : IRequestHandler<ConfirmTelecomOperationRequest, ConfirmTelecomOperationRequestResult>
{
    private readonly ITelecomActivationWorkflow _workflow;
    private readonly IUserAuditService _audit;
    private readonly IQueryContext _query;
    private readonly IOperatorContext _operator;
    private readonly ISellingLineEligibilityChecker _sellingLineEligibility;
    private readonly IConfirmTelecomOperationPermissionGate _permissionGate;
    private readonly IConfirmTelecomOperationSellingLineOverrideApplier _overrideApplier;

    public ConfirmTelecomOperationRequestHandler(
        ITelecomActivationWorkflow workflow,
        IUserAuditService audit,
        IQueryContext query,
        IOperatorContext operatorContext,
        ISellingLineEligibilityChecker sellingLineEligibility,
        IConfirmTelecomOperationPermissionGate permissionGate,
        IConfirmTelecomOperationSellingLineOverrideApplier overrideApplier)
    {
        _workflow = workflow;
        _audit = audit;
        _query = query;
        _operator = operatorContext;
        _sellingLineEligibility = sellingLineEligibility;
        _permissionGate = permissionGate;
        _overrideApplier = overrideApplier;
    }

    public async Task<ConfirmTelecomOperationRequestResult> Handle(
        ConfirmTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var operation = await LoadOperationAsync(request.Id, cancellationToken);

        await _permissionGate.EnsureCanConfirmAsync(operation, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.OverrideReasonCode))
        {
            await _overrideApplier.ApplyAsync(
                operation,
                request.Id,
                request.OverrideReasonCode,
                cancellationToken);
            operation = await LoadOperationAsync(request.Id, cancellationToken);
        }

        await _sellingLineEligibility.ValidateForConfirmAsync(operation, cancellationToken);

        var actorUserId = OperatorActor.RequireUserId(_operator);
        var result = await _workflow.ConfirmActivationAsync(request.Id, actorUserId, cancellationToken);

        await LogAuditAsync(operation, request.Id, actorUserId, result, cancellationToken);

        var op = result.Operation;
        var status = op?.Status ?? TelecomOperationStatus.Failed;
        var (hintAr, hintEn) = ConfirmTelecomOperationStatusHints.Map(result, status);

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

    private async Task<TelecomOperationRequest> LoadOperationAsync(string id, CancellationToken cancellationToken)
    {
        return await _query.TelecomOperationRequest.AsNoTracking()
            .Include(o => o.MsisdnAsset)
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");
    }

    private async Task LogAuditAsync(
        TelecomOperationRequest operation,
        string operationId,
        string actorUserId,
        TelecomActivationWorkflowResult result,
        CancellationToken cancellationToken)
    {
        var msisdn = operation.MsisdnAsset?.Msisdn ?? "—";
        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId,
                ActionType = UserAuditActionTypes.TelecomOperationConfirmed,
                EntityType = nameof(TelecomOperationRequest),
                EntityId = operationId,
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
                Payload = new { Id = operationId, operation.Kind, msisdn, result.IdempotentReplay, source = "Customer360" },
            },
            cancellationToken);
    }
}
