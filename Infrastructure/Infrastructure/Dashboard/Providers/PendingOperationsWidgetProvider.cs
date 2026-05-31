using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard.Providers;

public class PendingOperationsWidgetProvider : IDashboardWidgetDataProvider
{
    private readonly IQueryContext _query;

    public PendingOperationsWidgetProvider(IQueryContext query) => _query = query;

    public string ProviderKey => "pending_operations";

    public async Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var count = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(
                o => o.Status == TelecomOperationStatus.PendingDocuments
                    || o.Status == TelecomOperationStatus.Confirmed,
                cancellationToken);

        return new DashboardWidgetDataDto
        {
            ProviderKey = ProviderKey,
            ValueNumeric = count,
            ValueText = count.ToString("N0"),
            Status = count > 0 ? "warn" : "ok",
            Subtitle = "وثائق / تأكيد معلق",
        };
    }
}
