using Domain.Entities;

namespace Application.Common.Telecom.ChangeNumber;

public interface IChangeNumberEligibilityChecker
{
    Task<ChangeNumberEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string currentMsisdnAssetId,
        string targetMsisdnAssetId,
        string numberChangeReason,
        decimal? premiumFeeAmount,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default);

    Task<ChangeNumberEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}

public sealed record ChangeNumberEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? CustomerId,
    string? CurrentMsisdn,
    string? TargetMsisdn,
    string? PriorMsisdnAssetId,
    string? TargetMsisdnAssetId,
    bool RequiresBackOfficeApproval,
    string ValidationCode);

public static class ChangeNumberModes
{
    public const string Internal = "Internal";
}

public static class ChangeNumberWellKnown
{
    public static bool IsPremiumCategory(Domain.Enums.MsisdnCategory category) =>
        category is Domain.Enums.MsisdnCategory.Silver
            or Domain.Enums.MsisdnCategory.Gold
            or Domain.Enums.MsisdnCategory.Platinum;

    /// <summary>Premium MSISDN cleared when fee recorded or payment reference exists (BO gate).</summary>
    public static bool IsPremiumFeeSatisfied(
        Domain.Enums.MsisdnCategory category,
        decimal? premiumFeeAmount,
        string? paymentReference)
    {
        if (!IsPremiumCategory(category))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(paymentReference))
        {
            return true;
        }

        return premiumFeeAmount is > 0;
    }
}
