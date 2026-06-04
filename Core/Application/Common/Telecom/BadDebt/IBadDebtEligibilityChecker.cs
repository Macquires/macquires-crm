using Domain.Entities;

namespace Application.Common.Telecom.BadDebt;

public interface IBadDebtEligibilityChecker
{
    Task<BadDebtEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string collectionAction,
        string? dunningStage,
        string? paymentReference,
        decimal? collectedAmount,
        decimal? writeOffAmount,
        bool collectionApprovalConfirmed,
        string? priorDunningStage = null,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default);

    Task<BadDebtEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}

public sealed record BadDebtEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? CustomerId,
    string? Msisdn,
    string? MsisdnAssetId,
    decimal OutstandingBalanceSnapshot,
    bool RequiresBackOfficeApproval,
    string ValidationCode);

public static class BadDebtWellKnown
{
    public const string PaymentRecorded = "PaymentRecorded";
    public const string PaymentPlan = "PaymentPlan";
    public const string DunningEscalation = "DunningEscalation";
    public const string AgencyReferral = "AgencyReferral";
    public const string WriteOffPartial = "WriteOffPartial";
    public const string WriteOffFull = "WriteOffFull";

    public const string Reminder1 = "Reminder1";
    public const string Reminder2 = "Reminder2";
    public const string SoftBar = "SoftBar";
    public const string HardBar = "HardBar";
    public const string Agency = "Agency";
    public const string WriteOffPending = "WriteOffPending";
    public const string Settled = "Settled";

    public const string SettlementPending = "Pending";
    public const string SettlementCompleted = "Completed";
    public const string SettlementFailed = "Failed";

    public static bool IsKnownAction(string? action) =>
        string.Equals(action, PaymentRecorded, StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, PaymentPlan, StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, DunningEscalation, StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, AgencyReferral, StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, WriteOffPartial, StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, WriteOffFull, StringComparison.OrdinalIgnoreCase);

    public static bool RequiresBackOffice(string? action) =>
        string.Equals(action, AgencyReferral, StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, WriteOffPartial, StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, WriteOffFull, StringComparison.OrdinalIgnoreCase);

    public static bool IsKnownStage(string? stage) =>
        string.IsNullOrWhiteSpace(stage)
        || string.Equals(stage, Reminder1, StringComparison.OrdinalIgnoreCase)
        || string.Equals(stage, Reminder2, StringComparison.OrdinalIgnoreCase)
        || string.Equals(stage, SoftBar, StringComparison.OrdinalIgnoreCase)
        || string.Equals(stage, HardBar, StringComparison.OrdinalIgnoreCase)
        || string.Equals(stage, Agency, StringComparison.OrdinalIgnoreCase)
        || string.Equals(stage, WriteOffPending, StringComparison.OrdinalIgnoreCase)
        || string.Equals(stage, Settled, StringComparison.OrdinalIgnoreCase);
}
