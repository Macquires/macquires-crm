using Application.Common.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Dashboard;

/// <summary>Loads dashboard widget catalog at startup so the first page view does not hit SQL under client abort pressure.</summary>
public sealed class DashboardWidgetCatalogWarmupService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardWidgetCatalogWarmupService> _logger;

    public DashboardWidgetCatalogWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<DashboardWidgetCatalogWarmupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var catalog = scope.ServiceProvider.GetRequiredService<IDashboardWidgetCatalogReader>();
            await catalog.GetActiveWidgetsAsync(CancellationToken.None);
            _logger.LogInformation("Dashboard widget catalog warmed.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dashboard widget catalog warmup failed; will load on first request.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
