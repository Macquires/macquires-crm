using Application.Common.Dashboard;

namespace Infrastructure.Dashboard;

public class DashboardWidgetRegistry : IDashboardWidgetRegistry
{
    private readonly IReadOnlyDictionary<string, IDashboardWidgetDataProvider> _providers;

    public DashboardWidgetRegistry(IEnumerable<IDashboardWidgetDataProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> GetRegisteredProviderKeys() =>
        _providers.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

    public bool IsRegistered(string? providerKey) =>
        !string.IsNullOrWhiteSpace(providerKey) && _providers.ContainsKey(providerKey);

    public async Task<DashboardWidgetDataDto> GetDataAsync(string providerKey, CancellationToken cancellationToken = default)
    {
        if (!IsRegistered(providerKey))
        {
            return new DashboardWidgetDataDto
            {
                ProviderKey = providerKey ?? "",
                ValueText = "N/A",
                Status = "warn",
                Subtitle = "Provider not registered",
            };
        }

        // Dashboard stats are short-lived reads; do not tie to HTTP abort (duplicate navigations cancel the client).
        return await _providers[providerKey].GetAsync(CancellationToken.None);
    }
}
