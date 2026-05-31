using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Dashboard;

public sealed class DashboardWidgetCatalogReader : IDashboardWidgetCatalogReader
{
    public const string CacheKey = "dashboard:active-widgets";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim LoadGate = new(1, 1);

    private readonly IQueryContext _query;
    private readonly IMemoryCache _cache;

    public DashboardWidgetCatalogReader(IQueryContext query, IMemoryCache cache)
    {
        _query = query;
        _cache = cache;
    }

    public async Task<IReadOnlyList<DashboardWidget>> GetActiveWidgetsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<DashboardWidget>? cached) && cached != null)
        {
            return cached;
        }

        // Catalog is shared across requests — do not abort DB load when the HTTP client disconnects.
        await LoadGate.WaitAsync(CancellationToken.None);
        try
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null)
            {
                return cached;
            }

            var rows = await _query.DashboardWidget.AsNoTracking().IsDeletedEqualTo()
                .Where(w => w.IsActive)
                .OrderBy(w => w.SortOrder)
                .ThenBy(w => w.TitleAr)
                .ToListAsync(CancellationToken.None);

            var list = (IReadOnlyList<DashboardWidget>)rows;
            _cache.Set(CacheKey, list, CacheTtl);
            return list;
        }
        finally
        {
            LoadGate.Release();
        }
    }

    public void InvalidateCache() => _cache.Remove(CacheKey);
}
