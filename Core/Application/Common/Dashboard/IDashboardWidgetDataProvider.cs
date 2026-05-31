namespace Application.Common.Dashboard;

public interface IDashboardWidgetDataProvider
{
    string ProviderKey { get; }
    Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default);
}
