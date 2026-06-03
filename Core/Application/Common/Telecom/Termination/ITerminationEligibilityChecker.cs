using Domain.Entities;

namespace Application.Common.Telecom.Termination;

public interface ITerminationEligibilityChecker
{
    Task<TerminationEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string terminationType,
        string terminationReason,
        string? retentionOfferOutcome,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default);

    Task<TerminationEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}

public sealed record TerminationEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? CustomerId,
    string? Msisdn,
    string? MsisdnAssetId,
    string? PriorSimInventoryId,
    bool RequiresBackOfficeApproval,
    string ValidationCode);

public static class TerminationWellKnown
{
    public const string Voluntary = "Voluntary";
    public const string Collections = "Collections";
    public const string Regulatory = "Regulatory";
    public const string Fraud = "Fraud";

    public static bool IsBackOfficeType(string? terminationType) =>
        string.Equals(terminationType, Fraud, StringComparison.OrdinalIgnoreCase)
        || string.Equals(terminationType, Regulatory, StringComparison.OrdinalIgnoreCase)
        || string.Equals(terminationType, Collections, StringComparison.OrdinalIgnoreCase);

    public static bool IsKnownType(string? terminationType) =>
        string.Equals(terminationType, Voluntary, StringComparison.OrdinalIgnoreCase)
        || IsBackOfficeType(terminationType);
}
