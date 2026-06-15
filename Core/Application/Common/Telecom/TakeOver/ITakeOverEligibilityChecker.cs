using Domain.Entities;

namespace Application.Common.Telecom.TakeOver;

public interface ITakeOverEligibilityChecker
{
    Task<TakeOverEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string secondarySubscriberProfileId,
        string? msisdnAssetId,
        string transferReason,
        string? excludeOperationId = null,
        string? obligationSettlementReference = null,
        CancellationToken cancellationToken = default);

    void ValidateDocumentsForConfirm(TelecomOperationRequest operation);

    Task<TakeOverEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}

public sealed record TakeOverEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? OldCustomerId,
    string? NewCustomerId,
    string? Msisdn,
    string ValidationCode);
