using Application.Common.Integrations;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.DataAccessManager.EFCore.SchemaPatches;
using Infrastructure.SeedManager.Demos;
using Infrastructure.SeedManager.Systems;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        services.AddScoped<GeoCitySeeder>();
        services.AddScoped<TelecomSubscriptionTypeSeeder>();
        return services;
    }

    public static IHost SeedSystemData(this IHost host)
    {
        RunInSeedScope(host.Services, serviceProvider =>
        {
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
            serviceProvider.GetRequiredService<GeoCitySeeder>().GenerateDataAsync().Wait();
            serviceProvider.GetRequiredService<TelecomSubscriptionTypeSeeder>().EnsureReferenceDataAsync().Wait();
        });

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
        services.AddScoped<WorkforceDemoActivitySeeder>();
        services.AddScoped<DeviceInventorySeeder>();
        services.AddScoped<TelecomInHlrProvisioningLogSeeder>();
        return services;
    }

    public static IHost SeedDemoData(this IHost host)
    {
        RunInSeedScope(host.Services, serviceProvider =>
        {
            var context = serviceProvider.GetRequiredService<DataContext>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DI));

            MsisdnAssetSchemaPatches.EnsureIntendedSubscriptionTypeColumn(context, logger);
            RlsBranchIdSchemaPatches.EnsureBranchIdColumns(context, logger);

            serviceProvider.GetRequiredService<TelecomSubscriptionTypeSeeder>().EnsureReferenceDataAsync().Wait();
            serviceProvider.GetRequiredService<OrgUnitSeeder>().GenerateDataAsync().Wait();

            if (!context.Customer.IgnoreQueryFilters().Any(c => !c.IsDeleted))
            {
                serviceProvider.GetRequiredService<CustomerCategorySeeder>().GenerateDataAsync().Wait();
                serviceProvider.GetRequiredService<CustomerGroupSeeder>().GenerateDataAsync().Wait();
                serviceProvider.GetRequiredService<CustomerSeeder>().GenerateDataAsync().Wait();
                serviceProvider.GetRequiredService<CustomerContactSeeder>().GenerateDataAsync().Wait();
            }

            serviceProvider.GetRequiredService<ProductSeeder>().EnsureProductsAsync().Wait();
            serviceProvider.GetRequiredService<ProductCatalogSeeder>().GenerateDataAsync().Wait();
            serviceProvider.GetRequiredService<VasCatalogSeeder>().EnsureCatalogAsync().Wait();

            serviceProvider.GetRequiredService<IOracleInventoryIngestor>()
                .UpsertStandardCatalogAsync(resetExistingForDemo: true)
                .Wait();

            var telecomSeeder = serviceProvider.GetRequiredService<TelecomSyriatelSeeder>();
            telecomSeeder.GenerateDataAsync().Wait();
            telecomSeeder.EnsureDemoSimKitsAsync().Wait();
            telecomSeeder.EnsureAvailablePoolSimKitsAsync().Wait();
            telecomSeeder.EnsureHeroOfferSubscriptionDemoAsync().Wait();

            serviceProvider.GetRequiredService<TelecomCustomer360EnrichmentSeeder>().EnsureEnrichedAsync().Wait();
            serviceProvider.GetRequiredService<TelecomTechnicalTicketSeeder>().EnsureDemoTicketsAsync().Wait();
            serviceProvider.GetRequiredService<TelecomCustomer360EnrichmentSeeder>().EnsureEnrichedAsync().Wait();

            telecomSeeder.EnsureHeroReconnectDemoAsync().Wait();
            telecomSeeder.EnsureTribulationShowcaseDemoAsync().Wait();
            telecomSeeder.EnsureHeroBadDebtDemoAsync().Wait();

            serviceProvider.GetRequiredService<StrategicMisDemoSeeder>().GenerateDataAsync().Wait();
            serviceProvider.GetRequiredService<WorkforceDemoActivitySeeder>().GenerateDataAsync().Wait();
            serviceProvider.GetRequiredService<DeviceInventorySeeder>().EnsureDemoInventoryAsync().Wait();
            serviceProvider.GetRequiredService<TelecomInHlrProvisioningLogSeeder>().EnsureDemoLogsAsync().Wait();

            LogDemoSeedSummary(context, logger);
        });

        return host;
    }

    private static void RunInSeedScope(IServiceProvider root, Action<IServiceProvider> action)
    {
        using var scope = root.CreateScope();
        var gate = scope.ServiceProvider.GetRequiredService<ISeedExecutionGate>();
        using (gate.Enter())
        {
            action(scope.ServiceProvider);
        }
    }

    private static void LogDemoSeedSummary(DataContext context, ILogger logger)
    {
        var customers = context.Customer.IgnoreQueryFilters().Count(c => !c.IsDeleted);
        var profiles = context.SubscriberProfile.IgnoreQueryFilters().Count(p => !p.IsDeleted);
        var msisdns = context.MsisdnAsset.IgnoreQueryFilters().Count(m => !m.IsDeleted);
        var operations = context.TelecomOperationRequest.IgnoreQueryFilters().Count(o => !o.IsDeleted);
        var devices = context.DeviceInventory.IgnoreQueryFilters().Count(d => !d.IsDeleted);

        logger.LogInformation(
            "Demo seed complete — Customers={Customers}, SubscriberProfiles={Profiles}, MsisdnAssets={Msisdns}, Operations={Operations}, DeviceInventory={Devices}",
            customers,
            profiles,
            msisdns,
            operations,
            devices);
    }
}
