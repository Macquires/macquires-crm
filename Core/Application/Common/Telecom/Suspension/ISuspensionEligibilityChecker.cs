using Domain.Entities;

namespace Application.Common.Telecom.Suspension;

public interface ISuspensionEligibilityChecker
{
    Task<SuspensionEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string suspensionType,
        string suspensionReason,
        string barringLevel,
        bool autoReconnectEnabled,
        DateTime? suspensionEndDateUtc,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default);

    Task<SuspensionEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}

public sealed record SuspensionEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? CustomerId,
    string? Msisdn,
    string? MsisdnAssetId,
    bool RequiresBackOfficeApproval,
    string ValidationCode,
    bool LongSuspensionReviewTicket);

public static class SuspensionWellKnown
{
    public const string CustomerRequest = "CustomerRequest";
    public const string Billing = "Billing";
    public const string Fraud = "Fraud";
    public const string Regulatory = "Regulatory";
    public const string Operational = "Operational";

    public const string BarringFull = "Full";
    public const string BarringInboundOnly = "InboundOnly";
    public const string BarringOutboundOnly = "OutboundOnly";

    public static readonly TimeSpan LongSuspensionThreshold = TimeSpan.FromDays(90);

    public static bool IsBackOfficeType(string? suspensionType) =>
        string.Equals(suspensionType, Fraud, StringComparison.OrdinalIgnoreCase)
        || string.Equals(suspensionType, Regulatory, StringComparison.OrdinalIgnoreCase);

    public static bool IsKnownType(string? suspensionType) =>
        string.Equals(suspensionType, CustomerRequest, StringComparison.OrdinalIgnoreCase)
        || string.Equals(suspensionType, Billing, StringComparison.OrdinalIgnoreCase)
        || IsBackOfficeType(suspensionType)
        || string.Equals(suspensionType, Operational, StringComparison.OrdinalIgnoreCase);

    public static bool IsKnownBarringLevel(string? level) =>
        string.Equals(level, BarringFull, StringComparison.OrdinalIgnoreCase)
        || string.Equals(level, BarringInboundOnly, StringComparison.OrdinalIgnoreCase)
        || string.Equals(level, BarringOutboundOnly, StringComparison.OrdinalIgnoreCase);
}
