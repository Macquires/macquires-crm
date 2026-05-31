using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard.Providers;

public class ActiveSubscriptionsWidgetProvider : IDashboardWidgetDataProvider
{
    private readonly IQueryContext _query;

    public ActiveSubscriptionsWidgetProvider(IQueryContext query) => _query = query;

    public string ProviderKey => "active_subscriptions";

    public async Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var count = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo().CountAsync(cancellationToken);

        return new DashboardWidgetDataDto
        {
            ProviderKey = ProviderKey,
            ValueNumeric = count,
            ValueText = count.ToString("N0"),
            Status = "ok",
        };
    }
}
