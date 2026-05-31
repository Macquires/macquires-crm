using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard.Providers;

public class MsisdnAvailableWidgetProvider : IDashboardWidgetDataProvider
{
    private readonly IQueryContext _query;

    public MsisdnAvailableWidgetProvider(IQueryContext query) => _query = query;

    public string ProviderKey => "msisdn_available";

    public async Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var count = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(m => m.PoolStatus == MsisdnPoolStatus.Available, cancellationToken);

        return new DashboardWidgetDataDto
        {
            ProviderKey = ProviderKey,
            ValueNumeric = count,
            ValueText = count.ToString("N0"),
            Status = "ok",
            Subtitle = "أرقام متاحة في المخزون",
        };
    }
}
