namespace Application.Common.Dashboard;

public interface IDashboardWidgetRegistry
{
    IReadOnlyList<string> GetRegisteredProviderKeys();
    Task<DashboardWidgetDataDto> GetDataAsync(string providerKey, CancellationToken cancellationToken = default);
    bool IsRegistered(string? providerKey);
}
