using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard.Providers;

public class OperationsTodayWidgetProvider : IDashboardWidgetDataProvider
{
    private readonly IQueryContext _query;

    public OperationsTodayWidgetProvider(IQueryContext query) => _query = query;

    public string ProviderKey => "operations_today";

    public async Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);
        var count = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(
                o => o.CreatedAtUtc >= start && o.CreatedAtUtc < end,
                cancellationToken);

        return new DashboardWidgetDataDto
        {
            ProviderKey = ProviderKey,
            ValueNumeric = count,
            ValueText = count.ToString("N0"),
            Status = "ok",
            Subtitle = "عمليات أُنشئت اليوم (UTC)",
        };
    }
}
