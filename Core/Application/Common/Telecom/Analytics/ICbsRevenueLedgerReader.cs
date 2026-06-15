using Domain.Enums;

namespace Application.Common.Telecom.Analytics;

public sealed class CbsRevenueLedgerEntry
{
    public string CustomerId { get; init; } = null!;
    public decimal Amount { get; init; }
    public PaymentTransactionType TransactionType { get; init; }
    public string? BranchId { get; init; }
}

/// <summary>
/// Demo MIS revenue reader — local CRM payment ledger only (no external CBS revenue API).
/// </summary>
public interface ICbsRevenueLedgerReader
{
    Task<IReadOnlyList<CbsRevenueLedgerEntry>> ReadCompletedPaymentsAsync(
        IReadOnlyList<string> customerIds,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
