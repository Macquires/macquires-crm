using Application.Common.Telecom.Refund;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class RefundConfirmStrategy : IOperationConfirmStrategy
{
    private readonly IRefundEligibilityChecker _eligibility;
    private readonly IRefundCompletionService _completion;

    public RefundConfirmStrategy(
        IRefundEligibilityChecker eligibility,
        IRefundCompletionService completion)
    {
        _eligibility = eligibility;
        _completion = completion;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.DepositRefundSettlement;

    public bool RequiresNetworkProvision => false;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var check = await _eligibility.ValidateForConfirmAsync(entity, cancellationToken);
        return new(check.Allowed, check.MessageAr);
    }

    public async Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        await _completion.FulfillAsync(entity, actorUserId, cancellationToken);
        return new OperationApplyResult(null, null, null, entity.SubscriberProfileId, null, RequiresBilling: true);
    }
}
