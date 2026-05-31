using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard.Providers;

public class SubscriberCountWidgetProvider : IDashboardWidgetDataProvider
{
    private readonly IQueryContext _query;

    public SubscriberCountWidgetProvider(IQueryContext query) => _query = query;

    public string ProviderKey => "subscriber_count";

    public async Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _query.Customer.AsNoTracking().IsDeletedEqualTo().CountAsync(cancellationToken);
        var profiles = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo().CountAsync(cancellationToken);

        return new DashboardWidgetDataDto
        {
            ProviderKey = ProviderKey,
            ValueNumeric = customers,
            ValueText = customers.ToString("N0"),
            Subtitle = $"ملفات اشتراك: {profiles:N0}",
            Status = "ok",
        };
    }
}
