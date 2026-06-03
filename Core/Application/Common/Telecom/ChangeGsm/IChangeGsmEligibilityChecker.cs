using Domain.Entities;

namespace Application.Common.Telecom.ChangeGsm;

public interface IChangeGsmEligibilityChecker
{
    Task<ChangeGsmEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string? msisdnAssetId,
        string targetSubscriptionTypeId,
        string? migrationReason,
        CancellationToken cancellationToken = default);

    void EnsureMsisdnActive(MsisdnAsset? asset, string? msisdn);
}

public sealed record ChangeGsmEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? SourceSubscriptionTypeId,
    string? SourceTypeCode,
    string? TargetTypeCode,
    string CompatibilityStatus);
