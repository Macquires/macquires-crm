using Application.Common.Telecom.DeviceSales;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class DeviceSaleConfirmStrategy : IOperationConfirmStrategy
{
    private readonly IDeviceSalesEligibilityChecker _eligibility;
    private readonly IDeviceSaleCompletionService _completion;

    public DeviceSaleConfirmStrategy(
        IDeviceSalesEligibilityChecker eligibility,
        IDeviceSaleCompletionService completion)
    {
        _eligibility = eligibility;
        _completion = completion;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.DeviceSale;

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
