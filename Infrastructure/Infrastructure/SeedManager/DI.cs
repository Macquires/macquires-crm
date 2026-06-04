using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SeedManager.Demos;
using Infrastructure.SeedManager.Systems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.SeedManager;

public static class DI
{
    public static IServiceCollection RegisterSystemSeedManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<RoleSeeder>();
        services.AddScoped<UserAdminSeeder>();
        services.AddScoped<CompanySeeder>();
        services.AddScoped<TelecomDemoIdentitySeeder>();
        services.AddScoped<DashboardWidgetSeeder>();
        services.AddScoped<GlobalSettingSeeder>();
        services.AddScoped<OrgUnitSeeder>();
        services.AddScoped<RolePermissionSeeder>();
        return services;
    }

    public static IHost SeedSystemData(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var context = serviceProvider.GetRequiredService<DataContext>();
        var roleSeeder = serviceProvider.GetRequiredService<RoleSeeder>();
        var userAdminSeeder = serviceProvider.GetRequiredService<UserAdminSeeder>();

        if (!context.Roles.Any())
        {
            roleSeeder.GenerateDataAsync().Wait();
            userAdminSeeder.GenerateDataAsync().Wait();
            serviceProvider.GetRequiredService<CompanySeeder>().GenerateDataAsync().Wait();
        }
        else
        {
            roleSeeder.GenerateDataAsync().Wait();
        }

        userAdminSeeder.AssignAllCatalogRolesToDefaultAdminAsync().Wait();
        serviceProvider.GetRequiredService<TelecomDemoIdentitySeeder>().GenerateDataAsync().Wait();
        serviceProvider.GetRequiredService<DashboardWidgetSeeder>().GenerateDataAsync().Wait();
        serviceProvider.GetRequiredService<GlobalSettingSeeder>().GenerateDataAsync().Wait();
        serviceProvider.GetRequiredService<OrgUnitSeeder>().GenerateDataAsync().Wait();
        serviceProvider.GetRequiredService<RolePermissionSeeder>().GenerateDataAsync().Wait();
        return host;
    }

    public static IServiceCollection RegisterDemoSeedManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CustomerCategorySeeder>();
        services.AddScoped<CustomerGroupSeeder>();
        services.AddScoped<CustomerSeeder>();
        services.AddScoped<CustomerContactSeeder>();
        services.AddScoped<ProductSeeder>();
        services.AddScoped<TelecomSyriatelSeeder>();
        services.AddScoped<TelecomCustomer360EnrichmentSeeder>();
        services.AddScoped<ProductCatalogSeeder>();
        services.AddScoped<TelecomTechnicalTicketSeeder>();
        services.AddScoped<VasCatalogSeeder>();
        services.AddScoped<TelecomCustomer360EnrichmentSeeder>();
        services.AddScoped<StrategicMisDemoSeeder>();
        return services;
    }

    public static IHost SeedDemoData(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var context = serviceProvider.GetRequiredService<DataContext>();

        if (!context.Customer.Any())
        {
            serviceProvider.GetRequiredService<CustomerCategorySeeder>().GenerateDataAsync().Wait();
            serviceProvider.GetRequiredService<CustomerGroupSeeder>().GenerateDataAsync().Wait();
            serviceProvider.GetRequiredService<CustomerSeeder>().GenerateDataAsync().Wait();
            serviceProvider.GetRequiredService<CustomerContactSeeder>().GenerateDataAsync().Wait();
        }

        serviceProvider.GetRequiredService<ProductSeeder>().EnsureProductsAsync().Wait();

        if (!context.MsisdnAsset.Any(x => x.PoolStatus == Domain.Enums.MsisdnPoolStatus.Available))
        {
            serviceProvider.GetRequiredService<TelecomSyriatelSeeder>().GenerateDataAsync().Wait();
        }

        serviceProvider.GetRequiredService<TelecomSyriatelSeeder>().EnsureDemoSimKitsAsync().Wait();
        serviceProvider.GetRequiredService<TelecomSyriatelSeeder>().EnsureHeroOfferSubscriptionDemoAsync().Wait();
        serviceProvider.GetRequiredService<TelecomCustomer360EnrichmentSeeder>().EnsureEnrichedAsync().Wait();

        serviceProvider.GetRequiredService<ProductCatalogSeeder>().GenerateDataAsync().Wait();
        serviceProvider.GetRequiredService<TelecomTechnicalTicketSeeder>().EnsureDemoTicketsAsync().Wait();
        serviceProvider.GetRequiredService<VasCatalogSeeder>().EnsureCatalogAsync().Wait();
        serviceProvider.GetRequiredService<TelecomCustomer360EnrichmentSeeder>().EnsureEnrichedAsync().Wait();

        serviceProvider.GetRequiredService<TelecomSyriatelSeeder>().EnsureHeroReconnectDemoAsync().Wait();
        serviceProvider.GetRequiredService<TelecomSyriatelSeeder>().EnsureHeroBadDebtDemoAsync().Wait();

        serviceProvider.GetRequiredService<OrgUnitSeeder>().GenerateDataAsync().Wait();
        serviceProvider.GetRequiredService<StrategicMisDemoSeeder>().GenerateDataAsync().Wait();

        return host;
    }
}
