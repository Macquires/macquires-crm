using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard.Providers;

public class IntegrationHealthWidgetProvider : IDashboardWidgetDataProvider
{
    private readonly IQueryContext _query;

    public IntegrationHealthWidgetProvider(IQueryContext query) => _query = query;

    public string ProviderKey => "integration_health";

    public async Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var grouped = await _query.BillingIntegrationLog.AsNoTracking().IsDeletedEqualTo()
            .Where(l => l.CreatedAtUtc >= since)
            .GroupBy(l => l.IntegrationTarget ?? "Unknown")
            .Select(g => new
            {
                Target = g.Key,
                Total = g.Count(),
                Ok = g.Count(x => x.Success),
            })
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0)
        {
            return new DashboardWidgetDataDto
            {
                ProviderKey = ProviderKey,
                ValueText = "N/A",
                Status = "warn",
                Subtitle = "لا بيانات تكامل (7 أيام)",
            };
        }

        var items = grouped
            .OrderBy(g => g.Target)
            .Select(g =>
            {
                var pct = g.Total > 0 ? (int)Math.Round(100.0 * g.Ok / g.Total) : 0;
                return new DashboardWidgetDataItemDto(
                    g.Target,
                    $"{pct}% ({g.Ok}/{g.Total})",
                    pct >= 90 ? "ok" : pct >= 70 ? "warn" : "error");
            })
            .ToList();

        return new DashboardWidgetDataDto
        {
            ProviderKey = ProviderKey,
            ValueText = grouped.Count.ToString(),
            Subtitle = "أهداف تكامل نشطة",
            Status = items.Any(i => i.Status == "error") ? "error" : "ok",
            Items = items,
        };
    }
}
