using Application.Common.Dashboard;
using Infrastructure.Dashboard.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Dashboard;

public static class DI
{
    public static IServiceCollection RegisterDashboardEngine(this IServiceCollection services)
    {
        services.AddScoped<IDashboardWidgetDataProvider, SubscriberCountWidgetProvider>();
        services.AddScoped<IDashboardWidgetDataProvider, ActiveSubscriptionsWidgetProvider>();
        services.AddScoped<IDashboardWidgetDataProvider, PendingOperationsWidgetProvider>();
        services.AddScoped<IDashboardWidgetDataProvider, BulkImportActiveWidgetProvider>();
        services.AddScoped<IDashboardWidgetDataProvider, MsisdnAvailableWidgetProvider>();
        services.AddScoped<IDashboardWidgetDataProvider, OperationsTodayWidgetProvider>();
        services.AddScoped<IDashboardWidgetDataProvider, NetworkPulseWidgetProvider>();
        services.AddScoped<IDashboardWidgetDataProvider, IntegrationHealthWidgetProvider>();
        services.AddScoped<IDashboardWidgetDataProvider, BranchHeatWidgetProvider>();

        services.AddScoped<IDashboardWidgetRegistry, DashboardWidgetRegistry>();
        services.AddScoped<IDashboardWidgetCatalogReader, DashboardWidgetCatalogReader>();
        services.AddHostedService<DashboardWidgetCatalogWarmupService>();
        return services;
    }
}
