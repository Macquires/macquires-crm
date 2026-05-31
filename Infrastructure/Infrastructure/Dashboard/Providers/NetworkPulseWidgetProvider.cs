using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard.Providers;

public class NetworkPulseWidgetProvider : IDashboardWidgetDataProvider
{
    private readonly IQueryContext _query;

    public NetworkPulseWidgetProvider(IQueryContext query) => _query = query;

    public string ProviderKey => "network_pulse";

    public async Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var logs = await _query.BillingIntegrationLog.AsNoTracking().IsDeletedEqualTo()
            .Where(l => l.CreatedAtUtc >= since)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (logs.Count == 0)
        {
            return new DashboardWidgetDataDto
            {
                ProviderKey = ProviderKey,
                ValueText = "N/A",
                Subtitle = "لا سجلات تكامل خلال 24 ساعة",
                Status = "warn",
            };
        }

        var success = logs.Count(l => l.Success);
        var pct = (int)Math.Round(100.0 * success / logs.Count);
        var last = logs[0];

        return new DashboardWidgetDataDto
        {
            ProviderKey = ProviderKey,
            ValueText = $"{pct}%",
            ValueNumeric = pct,
            Subtitle = last.IntegrationTarget ?? "CBS/HLR",
            Status = pct >= 90 ? "ok" : pct >= 70 ? "warn" : "error",
            Items =
            [
                new DashboardWidgetDataItemDto("آخر محاولة", last.Success ? "نجاح" : "فشل", last.Success ? "ok" : "error"),
                new DashboardWidgetDataItemDto("محاولات 24س", logs.Count.ToString()),
            ],
        };
    }
}
