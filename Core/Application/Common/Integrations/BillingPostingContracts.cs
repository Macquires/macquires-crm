using Domain.Enums;

namespace Application.Common.Integrations;

public sealed record BillingJournalPostRequest(
    string OperationId,
    string OperationNumber,
    string? Msisdn,
    TelecomOperationKind Kind,
    decimal Amount,
    string JournalCode,
    string? CorrelationId = null,
    string? BranchId = null);

public sealed record BillingJournalPostResult(
    bool Success,
    string Message,
    string? JournalEntryId = null);

/// <summary>Posts financial journal entries to CBS/ERP (separate from subscriber provisioning).</summary>
public interface IBillingPostingIntegration
{
    Task<BillingJournalPostResult> PostJournalEntryAsync(
        BillingJournalPostRequest request,
        CancellationToken cancellationToken = default);
}
