using Domain.Entities;

namespace Application.Common.Dashboard;

/// <summary>Cached read of active dashboard widget definitions (reduces hot-path DB load).</summary>
public interface IDashboardWidgetCatalogReader
{
    Task<IReadOnlyList<DashboardWidget>> GetActiveWidgetsAsync(CancellationToken cancellationToken = default);

    void InvalidateCache();
}
