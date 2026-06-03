using Domain.Entities;

namespace Application.Common.Telecom.Reconnect;

public interface IReconnectEligibilityChecker
{
    Task<ReconnectEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string reconnectReason,
        string clearanceType,
        string? paymentReference,
        bool fraudClearanceConfirmed,
        string? sourceSuspensionOperationId,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default);

    Task<ReconnectEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}

public sealed record ReconnectEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? CustomerId,
    string? Msisdn,
    string? MsisdnAssetId,
    string? SourceSuspensionOperationId,
    bool RequiresBackOfficeApproval,
    string ValidationCode);

public static class ReconnectWellKnown
{
    public const string Payment = "Payment";
    public const string Fraud = "Fraud";
    public const string Regulatory = "Regulatory";
    public const string Customer = "Customer";
    public const string Operational = "Operational";

    public static bool IsKnownClearanceType(string? clearanceType) =>
        string.Equals(clearanceType, Payment, StringComparison.OrdinalIgnoreCase)
        || string.Equals(clearanceType, Fraud, StringComparison.OrdinalIgnoreCase)
        || string.Equals(clearanceType, Regulatory, StringComparison.OrdinalIgnoreCase)
        || string.Equals(clearanceType, Customer, StringComparison.OrdinalIgnoreCase)
        || string.Equals(clearanceType, Operational, StringComparison.OrdinalIgnoreCase);
}
