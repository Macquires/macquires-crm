using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Telecom.TakeOver;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public sealed class TakeOverCreateStrategy : IOperationCreateStrategy
{
    private readonly IPermissionEvaluator _permissions;
    private readonly ITakeOverEligibilityChecker _eligibility;

    public TakeOverCreateStrategy(
        IPermissionEvaluator permissions,
        ITakeOverEligibilityChecker eligibility)
    {
        _permissions = permissions;
        _eligibility = eligibility;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.TakeOver;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        await OperationCreatePermissionHelpers.EnsureTakeOverCreatePermissionAsync(
            context.ActorUserId,
            _permissions,
            cancellationToken);

        var result = await _eligibility.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.SecondarySubscriberProfileId!,
            request.MsisdnAssetId,
            request.TransferReason!,
            excludeOperationId: null,
            obligationSettlementReference: string.Equals(
                request.TakeOverObligationStatus,
                "Settled",
                StringComparison.OrdinalIgnoreCase)
                ? request.PaymentReference
                : null,
            cancellationToken: cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.TakeOverEligibility = result;
    }

    public Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.TakeOverEligibility;
        if (eligibility == null)
        {
            return Task.CompletedTask;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;

        entity.TransferReason = request.TransferReason!.Trim();
        entity.DepositTransferPolicy = request.DepositTransferPolicy ?? DepositTransferPolicy.TransferToNewOwner;
        entity.TakeOverEffectiveDateUtc = request.TakeOverEffectiveDateUtc ?? DateTime.UtcNow;
        entity.TakeOverObligationStatus = string.IsNullOrWhiteSpace(request.TakeOverObligationStatus)
            ? "Unknown"
            : request.TakeOverObligationStatus.Trim();
        entity.PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? null
            : request.PaymentReference.Trim();
        entity.ApprovalLevelRequired = "BackOffice";
        entity.OldCustomerId = eligibility.OldCustomerId;
        entity.NewCustomerId = eligibility.NewCustomerId;
        entity.PriorSubscriberProfileId = request.SubscriberProfileId;
        entity.Notes = OperationCreateAuditHelpers.AppendTakeOverAudit(entity.Notes, eligibility);
        return Task.CompletedTask;
    }
}
