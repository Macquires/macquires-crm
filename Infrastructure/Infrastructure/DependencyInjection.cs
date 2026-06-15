using Application.Common.Audit;
using Application.Common.Distributed;
using Application.Common.Services.FileImageManager;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Settings;
using Application.Common.Telecom;
using Application.Common.Telecom.Analytics;
using Infrastructure.Audit;
using Infrastructure.Dashboard;
using Infrastructure.Security;
using Infrastructure.Settings;
using Infrastructure.DataAccessManager.EFCore;
using Infrastructure.Security.FieldEncryption;
using Infrastructure.EmailManager;
using Infrastructure.FileDocumentManager;
using Infrastructure.FileImageManager;
using Infrastructure.LogManager.Serilogs;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Tokens;
using Infrastructure.SeedManager;
using Application.Common.Telecom.SellingLine;
using Infrastructure.Telecom;
using Infrastructure.Telecom.Analytics;
using Infrastructure.ExternalServices;
using Infrastructure.TelecomIntegrations;
using Infrastructure.TelecomIntegrations.BulkImport;
using Infrastructure.TelecomIntegrations.Oracle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;


public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        //>>> DataAccess
        services.RegisterDataAccess(configuration);

        //>>> Serilog
        services.RegisterSerilog(configuration);

        //>>> Token Manager
        services.RegisterToken(configuration);

        //>>> Security Manager
        services.RegisterSecurityManager(configuration);

        services.AddMemoryCache();
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
        services.AddScoped<IStorageProvider, LocalStorageProvider>();
        services.AddScoped<IGlobalSettingsProvider, GlobalSettingsProvider>();
        services.AddScoped<IUserScopeService, UserScopeService>();
        services.AddScoped<IStrategicDataScopeService, StrategicDataScopeService>();
        services.AddScoped<IOperationalAnalyticsScopeService, OperationalAnalyticsScopeService>();
        services.AddScoped<IExecutiveFinancialMetricsService, ExecutiveFinancialMetricsService>();
        services.AddScoped<IWorkforceUserReadService, WorkforceUserReadService>();
        services.AddScoped<IWorkforceAnalyticsScopeService, WorkforceAnalyticsScopeService>();
        services.AddScoped<ICbsRevenueLedgerReader, LocalCbsRevenueLedgerReader>();
        services.AddScoped<OperatorContext>();
        services.AddScoped<SeedOperatorContext>();
        services.AddScoped<SystemOperatorContext>();
        services.AddScoped<ISeedExecutionGate, SeedExecutionGate>();
        services.AddScoped<ISystemExecutionGate, SystemExecutionGate>();
        services.AddScoped<IOperatorContext>(sp =>
        {
            if (sp.GetRequiredService<ISeedExecutionGate>().IsActive)
            {
                return sp.GetRequiredService<SeedOperatorContext>();
            }

            if (sp.GetRequiredService<ISystemExecutionGate>().IsActive)
            {
                return sp.GetRequiredService<SystemOperatorContext>();
            }

            return sp.GetRequiredService<OperatorContext>();
        });
        services.AddScoped<IActivationChannelContext, ActivationChannelContext>();
        services.AddScoped<IDealerCodeValidator, DealerCodeValidator>();
        services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
        services.AddScoped<IPersonaStrictGate, PersonaStrictGate>();
        services.AddScoped<IGlobalSettingsAdminService, GlobalSettingsAdminService>();
        services.AddScoped<IntegrationEnablement>();
        services.AddScoped<IUserAuditService, UserAuditService>();
        services.AddScoped<IUserAuditReadService, UserAuditReadService>();

        services.RegisterDashboardEngine();

        //>>> System Seed Manager
        services.RegisterSystemSeedManager(configuration);

        //>>> Demo Seed Manager
        services.RegisterDemoSeedManager(configuration);

        //>>> DeletedById Manager
        services.RegisterEmailManager(configuration);

        //>>> FileDocumentManager
        services.RegisterFileDocumentManager(configuration);

        //>>> FileImageManager
        services.RegisterFileImageManager(configuration);

        services.Configure<FieldEncryptionOptions>(configuration.GetSection(FieldEncryptionOptions.SectionName));
        services.AddSingleton<IFieldEncryptionService, AesGcmFieldEncryptionService>();
        services.AddScoped<INationalIdSearchHashBackfillService, NationalIdSearchHashBackfillService>();

        services.Configure<TelecomBillingOptions>(configuration.GetSection(TelecomBillingOptions.SectionName));
        services.AddTelecomHttpClients(configuration);
        services.AddTelecomIntegrationMessaging(configuration);
        services.AddScoped<Application.Common.Integrations.IIntegrationOutbox, TelecomIntegrations.Outbox.IntegrationOutboxService>();
        services.AddScoped<ITelecomProvisionedEventDispatcher, TelecomIntegrations.Outbox.TelecomProvisionedEventDispatcher>();
        services.AddScoped<Application.Common.Integrations.IIdempotencyStore, TelecomIntegrations.Idempotency.SqlIdempotencyStore>();
        services.AddHostedService<TelecomIntegrations.Outbox.IntegrationOutboxDispatcherHostedService>();
        services.AddScoped<Application.Common.Telecom.OperationConfirm.IOperationConfirmProvisionContextBuilder, Application.Common.Telecom.OperationConfirm.OperationConfirmProvisionContextBuilder>();
        RegisterOperationConfirmStrategies(services);
        RegisterOperationCreateStrategies(services);
        services.AddScoped<Application.Common.Telecom.OperationConfirm.IOperationConfirmStrategyRegistry, Application.Common.Telecom.OperationConfirm.OperationConfirmStrategyRegistry>();
        services.AddScoped<Application.Common.Telecom.OperationCreate.IOperationCreateStrategyRegistry, Application.Common.Telecom.OperationCreate.OperationCreateStrategyRegistry>();
        services.AddScoped<Application.Common.Telecom.OperationCreate.IOperationCreatePostCreateService, Application.Common.Telecom.OperationCreate.OperationCreatePostCreateService>();
        services.AddScoped<Application.Common.Telecom.RevenueAssurance.IRevenueAssuranceLeakageScanner, TelecomIntegrations.RevenueAssuranceLeakageScanner>();
        services.AddSingleton<IDistributedLock, RedisDistributedLock>();
        services.AddSingleton<ITelecomOperationDocumentStore, TelecomOperationDocumentStore>();
        services.Configure<KycDocumentStorageOptions>(configuration.GetSection(KycDocumentStorageOptions.SectionName));
        services.AddSingleton<IKycDocumentStorageService, FileSystemKycDocumentStorageService>();
        services.AddScoped<IBillingSystemIntegration, HuaweiCbsBillingIntegration>();
        services.AddTelecomIntegrationAdapters(configuration);
        services.AddScoped<ITelecomIntegrationLogWriter, TelecomIntegrationLogWriter>();
        services.AddScoped<INetworkProvisioningService, HlrNetworkProvisioningService>();
        services.AddScoped<IPendingExternalSyncService, PendingExternalSyncService>();
        services.AddScoped<IVasProvisioningService, HlrVasProvisioningService>();
        services.AddScoped<IHLRLiveStatusService, HlrLiveStatusService>();
        services.RegisterBulkImport(configuration);
        services.AddHostedService<InventoryBulkImportBackgroundService>();
        services.AddScoped<ITelecomDirectorySync, TelecomDirectoryMockSyncIntegration>();
        services.AddScoped<IChargingSystemIntegration, ChargingSystemMockIntegration>();
        services.AddScoped<ISmsGatewayIntegration, SmsGatewayMockIntegration>();
        services.AddScoped<IVasBillingIntegration, VasBillingMockIntegration>();
        services.AddScoped<IBillingPostingIntegration, BillingPostingMockIntegration>();
        services.AddScoped<ITakeOverObligationSettlementIntegration, TakeOverObligationSettlementMockIntegration>();
        RegisterMnpPortabilityGateway(services, configuration);
        services.AddHostedService<DeviceInstallmentDelinquencyHostedService>();
        services.AddSingleton<Application.Common.Services.ProductCatalog.IActiveProductCatalogCache, Services.ProductCatalog.ActiveProductCatalogCache>();
        services.AddScoped<IOracleFusionInventoryClient, OracleFusionInventoryMockClient>();
        services.AddScoped<IOracleInventoryIngestor, OracleInventoryIngestor>();
        services.AddScoped<IOracleInventorySyncService, OracleInventorySyncService>();
        services.AddHostedService<MsisdnReservationCleanupService>();
        services.AddHostedService<MsisdnQuarantineRecyclingHostedService>();
        services.AddHostedService<DormantLineScannerHostedService>();
        services.AddHostedService<SuspensionAutoReconnectHostedService>();
        services.AddHostedService<ScheduledTelecomOperationHostedService>();
        services.AddHostedService<TelecomProvisioningJobHostedService>();
        services.AddHostedService<OracleInventorySyncHostedService>();
        services.AddHostedService<RevenueAssuranceReconciliationJob>();
        services.AddHostedService<Telecom.Analytics.ExecutiveWeeklyDigestEmailHostedService>();

        return services;
    }

    private static void RegisterMnpPortabilityGateway(IServiceCollection services, IConfiguration configuration)
    {
        var mode = configuration.GetValue("TelecomIntegrations:Mnp:Mode", "Mock");
        if (string.Equals(mode, "Http", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IMnpPortabilityGateway, Infrastructure.TelecomIntegrations.Http.MnpHttpGateway>();
        }
        else
        {
            services.AddScoped<IMnpPortabilityGateway, MnpPortabilityMockGateway>();
        }
    }

    private static void RegisterOperationConfirmStrategies(IServiceCollection services)
    {
        var strategyAssembly = typeof(Application.Common.Telecom.OperationConfirm.IOperationConfirmStrategy).Assembly;
        foreach (var type in strategyAssembly.GetTypes())
        {
            if (type is { IsAbstract: false, IsInterface: false }
                && typeof(Application.Common.Telecom.OperationConfirm.IOperationConfirmStrategy).IsAssignableFrom(type))
            {
                services.AddScoped(typeof(Application.Common.Telecom.OperationConfirm.IOperationConfirmStrategy), type);
            }
        }
    }

    private static void RegisterOperationCreateStrategies(IServiceCollection services)
    {
        var strategyAssembly = typeof(Application.Common.Telecom.OperationCreate.IOperationCreateStrategy).Assembly;
        foreach (var type in strategyAssembly.GetTypes())
        {
            if (type is { IsAbstract: false, IsInterface: false }
                && typeof(Application.Common.Telecom.OperationCreate.IOperationCreateStrategy).IsAssignableFrom(type))
            {
                services.AddScoped(typeof(Application.Common.Telecom.OperationCreate.IOperationCreateStrategy), type);
            }
        }
    }
}


