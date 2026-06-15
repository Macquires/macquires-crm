using Application.Common.CQS.Queries;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telecom.Analytics;

public sealed class ExecutiveFinancialMetricsService : IExecutiveFinancialMetricsService
{
    private static readonly SubscriberOperationalStatus[] ChurnStatuses =
    [
        SubscriberOperationalStatus.Terminated,
        SubscriberOperationalStatus.Deactivated,
        SubscriberOperationalStatus.Suspended,
        SubscriberOperationalStatus.SuspendedInbound,
        SubscriberOperationalStatus.SuspendedOutbound,
    ];

    private readonly IQueryContext _context;
    private readonly ICbsRevenueLedgerReader _revenueLedger;

    public ExecutiveFinancialMetricsService(
        IQueryContext context,
        ICbsRevenueLedgerReader revenueLedger)
    {
        _context = context;
        _revenueLedger = revenueLedger;
    }

    public async Task<ExecutiveFinancialMetrics> ComputeAsync(
        IReadOnlyList<string> branchIds,
        DateTime periodToUtc,
        int billingCycleDays = 30,
        CancellationToken cancellationToken = default)
    {
        if (branchIds.Count == 0)
        {
            return new ExecutiveFinancialMetrics();
        }

        var periodEnd = periodToUtc;
        var periodStart = periodEnd.AddDays(-billingCycleDays);
        var priorStart = periodStart.AddDays(-billingCycleDays);

        var customerIds = await ResolveCustomerIdsAsync(branchIds, cancellationToken);
        if (customerIds.Count == 0)
        {
            return new ExecutiveFinancialMetrics();
        }

        var currentPayments = await LoadCompletedPaymentsAsync(
            customerIds, periodStart, periodEnd, cancellationToken);
        var priorPayments = await LoadCompletedPaymentsAsync(
            customerIds, priorStart, periodStart, cancellationToken);

        var totalRevenue = currentPayments.Sum(p => p.Amount);
        var priorRevenue = priorPayments.Sum(p => p.Amount);
        var payingCustomers = currentPayments.Select(p => p.CustomerId).Distinct().Count();
        var arpu = payingCustomers > 0
            ? decimal.Round(totalRevenue / payingCustomers, 2)
            : 0m;

        var revenueChangePercent = priorRevenue > 0
            ? decimal.Round((totalRevenue - priorRevenue) / priorRevenue * 100m, 1)
            : totalRevenue > 0 ? 100m : 0m;

        var churn30 = await ComputeChurnPercentAsync(customerIds, branchIds, 30, cancellationToken);
        var churn60 = await ComputeChurnPercentAsync(customerIds, branchIds, 60, cancellationToken);

        var branchHeat = await BuildBranchHeatAsync(
            branchIds, customerIds, periodStart, periodEnd, cancellationToken);
        var revenueByCategory = BuildRevenueByCategory(currentPayments);
        var revenueTrend = await BuildRevenueTrendAsync(
            customerIds, periodEnd, cancellationToken);

        return new ExecutiveFinancialMetrics
        {
            Arpu = arpu,
            ChurnPercent30 = churn30,
            ChurnPercent60 = churn60,
            TotalRevenue = decimal.Round(totalRevenue, 0),
            RevenueChangePercent = revenueChangePercent,
            BranchHeat = branchHeat,
            RevenueByCategory = revenueByCategory,
            RevenueTrend = revenueTrend,
        };
    }

    private async Task<List<string>> ResolveCustomerIdsAsync(
        IReadOnlyList<string> branchIds,
        CancellationToken cancellationToken) =>
        await _context.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted
                && c.OrgUnitId != null
                && branchIds.Contains(c.OrgUnitId))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

    private async Task<List<PaymentRow>> LoadCompletedPaymentsAsync(
        List<string> customerIds,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        if (customerIds.Count == 0)
        {
            return [];
        }

        return (await _revenueLedger.ReadCompletedPaymentsAsync(
                customerIds, fromUtc, toUtc, cancellationToken))
            .Select(p => new PaymentRow(
                p.CustomerId,
                p.Amount,
                p.TransactionType,
                p.BranchId))
            .ToList();
    }

    private static List<ExecutiveRevenueCategoryDto> BuildRevenueByCategory(List<PaymentRow> payments)
    {
        if (payments.Count == 0)
        {
            return [];
        }

        var total = payments.Sum(p => p.Amount);
        if (total <= 0)
        {
            return [];
        }

        return payments
            .GroupBy(p => MapCategory(p.TransactionType))
            .Select(g => new ExecutiveRevenueCategoryDto
            {
                Category = g.Key,
                Amount = decimal.Round(g.Sum(x => x.Amount), 0),
                Percent = decimal.Round(g.Sum(x => x.Amount) / total * 100m, 1),
            })
            .OrderByDescending(x => x.Amount)
            .ToList();
    }

    private static string MapCategory(PaymentTransactionType type) => type switch
    {
        PaymentTransactionType.Recharge => "شحن رصيد",
        PaymentTransactionType.VoucherRedeem => "قسائم",
        PaymentTransactionType.BillPay => "دفع فواتير",
        PaymentTransactionType.WalletTopUp => "محفظة",
        PaymentTransactionType.ActivationDeposit => "تأمينات تفعيل",
        _ => "أخرى",
    };

    private async Task<decimal> ComputeChurnPercentAsync(
        List<string> customerIds,
        IReadOnlyList<string> branchIds,
        int windowDays,
        CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow.AddDays(-windowDays);
        var prolongedCutoff = DateTime.UtcNow.AddDays(-30);

        var activeProfiles = await _context.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted
                && customerIds.Contains(p.CustomerId)
                && p.OperationalStatus == SubscriberOperationalStatus.Active)
            .CountAsync(cancellationToken);

        var terminatedByOp = await (
            from o in _context.TelecomOperationRequest.AsNoTracking()
            join p in _context.SubscriberProfile.AsNoTracking() on o.SubscriberProfileId equals p.Id
            where !o.IsDeleted && !p.IsDeleted
                && customerIds.Contains(p.CustomerId)
                && o.Kind == TelecomOperationKind.Termination
                && o.Status == TelecomOperationStatus.Completed
                && o.ConfirmedAtUtc >= windowStart
                && (o.BranchId == null || branchIds.Contains(o.BranchId))
            select o.SubscriberProfileId).Distinct().CountAsync(cancellationToken);

        var statusChurned = await _context.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted
                && customerIds.Contains(p.CustomerId)
                && (p.OperationalStatus == SubscriberOperationalStatus.Terminated
                    || p.OperationalStatus == SubscriberOperationalStatus.Deactivated)
                && p.UpdatedAtUtc >= windowStart)
            .CountAsync(cancellationToken);

        var prolongedSuspension = await _context.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted
                && customerIds.Contains(p.CustomerId)
                && (p.OperationalStatus == SubscriberOperationalStatus.Suspended
                    || p.OperationalStatus == SubscriberOperationalStatus.SuspendedInbound
                    || p.OperationalStatus == SubscriberOperationalStatus.SuspendedOutbound)
                && p.UpdatedAtUtc <= prolongedCutoff
                && p.UpdatedAtUtc >= windowStart)
            .CountAsync(cancellationToken);

        var churned = terminatedByOp + statusChurned + prolongedSuspension;
        var baseCount = activeProfiles + churned;
        return baseCount > 0
            ? decimal.Round((decimal)churned / baseCount * 100m, 2)
            : 0m;
    }

    private async Task<List<ExecutiveBranchHeatDto>> BuildBranchHeatAsync(
        IReadOnlyList<string> branchIds,
        List<string> customerIds,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken)
    {
        var branches = await _context.OrgUnit.AsNoTracking()
            .Where(b => branchIds.Contains(b.Id) && !b.IsDeleted)
            .Select(b => new { b.Id, b.NameAr })
            .ToListAsync(cancellationToken);

        var payments = await _context.TelecomPaymentTransaction.AsNoTracking()
            .Where(p => !p.IsDeleted
                && !p.IsReversal
                && p.Status == PaymentTransactionStatus.Completed
                && p.ConfirmedAtUtc >= periodStart
                && p.ConfirmedAtUtc <= periodEnd
                && customerIds.Contains(p.CustomerId))
            .Select(p => new { p.CustomerId, p.Amount, p.BranchId })
            .ToListAsync(cancellationToken);

        var customerBranch = await _context.Customer.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id) && c.OrgUnitId != null)
            .Select(c => new { c.Id, BranchId = c.OrgUnitId! })
            .ToDictionaryAsync(x => x.Id, x => x.BranchId, cancellationToken);

        var subsPerBranch = await (
            from s in _context.TelecomSubscription.AsNoTracking()
            join p in _context.SubscriberProfile.AsNoTracking() on s.SubscriberProfileId equals p.Id
            join c in _context.Customer.AsNoTracking() on p.CustomerId equals c.Id
            where !s.IsDeleted && !p.IsDeleted && !c.IsDeleted
                && c.OrgUnitId != null
                && branchIds.Contains(c.OrgUnitId)
            group s by c.OrgUnitId into g
            select new { BranchId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.BranchId, x => x.Count, cancellationToken);

        var revenueByBranch = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var p in payments)
        {
            var branchKey = !string.IsNullOrEmpty(p.BranchId) && branchIds.Contains(p.BranchId)
                ? p.BranchId
                : customerBranch.GetValueOrDefault(p.CustomerId);
            if (string.IsNullOrEmpty(branchKey))
            {
                continue;
            }

            revenueByBranch.TryGetValue(branchKey, out var current);
            revenueByBranch[branchKey] = current + p.Amount;
        }

        return branches
            .Select(b => new ExecutiveBranchHeatDto
            {
                BranchId = b.Id,
                BranchName = b.NameAr,
                Revenue = decimal.Round(revenueByBranch.GetValueOrDefault(b.Id), 0),
                ActiveSubscriptions = subsPerBranch.GetValueOrDefault(b.Id),
            })
            .OrderByDescending(x => x.Revenue)
            .Take(12)
            .ToList();
    }

    private async Task<List<ExecutiveRevenueTrendPointDto>> BuildRevenueTrendAsync(
        List<string> customerIds,
        DateTime periodEndUtc,
        CancellationToken cancellationToken)
    {
        var trendStart = new DateTime(periodEndUtc.Year, periodEndUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-5);

        var rows = await _context.TelecomPaymentTransaction.AsNoTracking()
            .Where(p => !p.IsDeleted
                && !p.IsReversal
                && p.Status == PaymentTransactionStatus.Completed
                && p.ConfirmedAtUtc >= trendStart
                && p.ConfirmedAtUtc <= periodEndUtc
                && customerIds.Contains(p.CustomerId))
            .Select(p => new { p.Amount, p.ConfirmedAtUtc })
            .ToListAsync(cancellationToken);

        var arabicMonths = new[] { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
        var points = new List<ExecutiveRevenueTrendPointDto>();

        for (var i = 0; i < 6; i++)
        {
            var monthStart = trendStart.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            var sum = rows
                .Where(r => r.ConfirmedAtUtc >= monthStart && r.ConfirmedAtUtc < monthEnd)
                .Sum(r => r.Amount);
            points.Add(new ExecutiveRevenueTrendPointDto
            {
                Label = arabicMonths[monthStart.Month - 1],
                Value = decimal.Round(sum, 0),
            });
        }

        return points;
    }

    private sealed record PaymentRow(
        string CustomerId,
        decimal Amount,
        PaymentTransactionType TransactionType,
        string? BranchId);
}
