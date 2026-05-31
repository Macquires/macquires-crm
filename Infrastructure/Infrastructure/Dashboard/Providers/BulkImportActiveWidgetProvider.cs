using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard.Providers;

public class BulkImportActiveWidgetProvider : IDashboardWidgetDataProvider
{
    private readonly IQueryContext _query;

    public BulkImportActiveWidgetProvider(IQueryContext query) => _query = query;

    public string ProviderKey => "bulk_import_active";

    public async Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var count = await _query.InventoryBulkImportJob.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(
                j => j.JobStatus == InventoryBulkImportJobStatus.Pending
                    || j.JobStatus == InventoryBulkImportJobStatus.Processing,
                cancellationToken);

        return new DashboardWidgetDataDto
        {
            ProviderKey = ProviderKey,
            ValueNumeric = count,
            ValueText = count.ToString("N0"),
            Status = count > 0 ? "warn" : "ok",
        };
    }
}
