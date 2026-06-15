using Domain.Entities;

namespace Application.Common.Telecom.OfferSubscription;

public enum OfferSubscriptionIntent
{
    BasePackageMigration = 0,
    VasActivate = 1,
    VasDeactivate = 2,
}

public interface IOfferSubscriptionEligibilityChecker
{
    Task<OfferSubscriptionEligibilityResult> ValidateForMigrationCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string productOfferingId,
        string resolvedProductId,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default);

    Task<OfferSubscriptionEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);

    Task<OfferSubscriptionEligibilityResult> ValidateForVasToggleAsync(
        string msisdn,
        string serviceCode,
        bool activate,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default);
}

public sealed record OfferSubscriptionEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? Msisdn,
    string? MsisdnAssetId,
    string? PriorProductId,
    string? PriorProductOfferingId,
    string ValidationCode);

public static class OfferSubscriptionWellKnown
{
    public const string ValPrefix = "VAL-11";

    public static string Format(string code, string messageAr) =>
        $"{ValPrefix}-{code}: {messageAr}";
}
