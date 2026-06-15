namespace Application.Common.Telecom.Analytics;

public interface IExecutiveFinancialMetricsService
{
    Task<ExecutiveFinancialMetrics> ComputeAsync(
        IReadOnlyList<string> branchIds,
        DateTime periodToUtc,
        int billingCycleDays = 30,
        CancellationToken cancellationToken = default);
}
