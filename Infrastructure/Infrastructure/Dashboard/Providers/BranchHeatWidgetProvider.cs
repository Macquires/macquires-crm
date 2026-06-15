using Application.Common.Dashboard;
using Application.Common.Telecom.Analytics;

namespace Infrastructure.Dashboard.Providers;

/// <summary>Top branches by revenue for executive Bento widgets.</summary>
public sealed class BranchHeatWidgetProvider : IDashboardWidgetDataProvider
{
    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IExecutiveFinancialMetricsService _financialMetrics;

    public BranchHeatWidgetProvider(
        IOperationalAnalyticsScopeService scopeService,
        IExecutiveFinancialMetricsService financialMetrics)
    {
        _scopeService = scopeService;
        _financialMetrics = financialMetrics;
    }

    public string ProviderKey => "branch_heat_top";

    public async Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var scope = await _scopeService.ResolveScopeAsync(null, null, cancellationToken);
        if (scope.EffectiveBranchIds.Count == 0)
        {
            return new DashboardWidgetDataDto
            {
                ProviderKey = ProviderKey,
                ValueText = "—",
                Subtitle = scope.ScopeLabelAr,
                Status = "warn",
            };
        }

        var metrics = await _financialMetrics.ComputeAsync(
            scope.EffectiveBranchIds,
            DateTime.UtcNow,
            30,
            cancellationToken);

        var top = metrics.BranchHeat
            .OrderByDescending(b => b.Revenue)
            .Take(5)
            .ToList();

        if (top.Count == 0)
        {
            return new DashboardWidgetDataDto
            {
                ProviderKey = ProviderKey,
                ValueText = "0",
                Subtitle = "لا إيراد مسجّل",
                Status = "warn",
            };
        }

        var total = top.Sum(b => b.Revenue);
        return new DashboardWidgetDataDto
        {
            ProviderKey = ProviderKey,
            ValueText = $"{total:N0} ل.س",
            Subtitle = $"أعلى {top.Count} فروع — {scope.ScopeLabelAr}",
            Status = "ok",
            Items = top.Select(b => new DashboardWidgetDataItemDto(
                b.BranchName,
                $"{b.Revenue:N0} ل.س",
                b.Revenue > 0 ? "ok" : "warn")).ToList(),
        };
    }
}
