using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telecom.Analytics;

/// <summary>
/// Demo MIS revenue source — reads completed payments from the local CRM ledger (seeded demo data).
/// No external CBS HTTP calls; operational CBS remains <see cref="TelecomIntegrations.HuaweiCbsBillingIntegration"/> mock/simulator.
/// </summary>
public sealed class LocalCbsRevenueLedgerReader : ICbsRevenueLedgerReader
{
    private readonly IQueryContext _context;

    public LocalCbsRevenueLedgerReader(IQueryContext context) => _context = context;

    public async Task<IReadOnlyList<CbsRevenueLedgerEntry>> ReadCompletedPaymentsAsync(
        IReadOnlyList<string> customerIds,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (customerIds.Count == 0)
        {
            return [];
        }

        return await _context.TelecomPaymentTransaction.AsNoTracking()
            .Where(p => !p.IsDeleted
                && !p.IsReversal
                && p.Status == PaymentTransactionStatus.Completed
                && p.ConfirmedAtUtc >= fromUtc
                && p.ConfirmedAtUtc <= toUtc
                && customerIds.Contains(p.CustomerId))
            .Select(p => new CbsRevenueLedgerEntry
            {
                CustomerId = p.CustomerId,
                Amount = p.Amount,
                TransactionType = p.TransactionType,
                BranchId = p.BranchId,
            })
            .ToListAsync(cancellationToken);
    }
}
