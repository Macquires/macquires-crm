using Application.Common.Audit;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Settings;
using Application.Common.Telecom;
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
using Infrastructure.TelecomIntegrations;
using Infrastructure.TelecomIntegrations.BulkImport;
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
        services.AddScoped<IGlobalSettingsProvider, GlobalSettingsProvider>();
        services.AddScoped<IUserScopeService, UserScopeService>();
        services.AddScoped<IStrategicDataScopeService, StrategicDataScopeService>();
        services.AddScoped<IOperatorContext, OperatorContext>();
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

        services.Configure<TelecomBillingOptions>(configuration.GetSection(TelecomBillingOptions.SectionName));
        services.AddSingleton<ITelecomOperationDocumentStore, TelecomOperationDocumentStore>();
        services.AddScoped<IBillingSystemIntegration, HuaweiCbsBillingIntegration>();
        services.AddScoped<ITelecomIntegrationLogWriter, TelecomIntegrationLogWriter>();
        services.AddScoped<INetworkProvisioningService, HlrNetworkProvisioningService>();
        services.AddScoped<IPendingExternalSyncService, PendingExternalSyncService>();
        services.AddScoped<IVasProvisioningService, HlrVasProvisioningService>();
        services.AddScoped<IHLRLiveStatusService, HlrLiveStatusService>();
        services.AddScoped<IESimDpPlusService, ESimDpPlusMockService>();
        services.RegisterBulkImport(configuration);
        services.AddHostedService<InventoryBulkImportBackgroundService>();
        services.AddScoped<ITelecomDirectorySync, TelecomDirectoryMockSyncIntegration>();
        services.AddScoped<IChargingSystemIntegration, ChargingSystemMockIntegration>();
        services.AddScoped<ISmsGatewayIntegration, SmsGatewayMockIntegration>();
        services.AddScoped<IPaymentGatewayIntegration, PaymentGatewayMockIntegration>();
        services.AddScoped<IDeviceInventoryIntegration, DeviceInventoryMockIntegration>();
        services.AddHostedService<DeviceInstallmentDelinquencyHostedService>();
        services.AddSingleton<Application.Common.Services.ProductCatalog.IActiveProductCatalogCache, Services.ProductCatalog.ActiveProductCatalogCache>();
        services.AddHostedService<MsisdnReservationCleanupService>();
        services.AddHostedService<SuspensionAutoReconnectHostedService>();

        return services;
    }
}


