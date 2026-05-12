using Application.Common.Integrations;
using Infrastructure.DataAccessManager.EFCore;
using Infrastructure.EmailManager;
using Infrastructure.FileDocumentManager;
using Infrastructure.FileImageManager;
using Infrastructure.LogManager.Serilogs;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Tokens;
using Infrastructure.SeedManager;
using Infrastructure.TelecomIntegrations;
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

        services.Configure<TelecomBillingOptions>(configuration.GetSection(TelecomBillingOptions.SectionName));
        services.AddScoped<IBillingSystemIntegration, HuaweiCbsBillingIntegration>();
        services.AddScoped<IChargingSystemIntegration, ChargingSystemMockIntegration>();
        services.AddScoped<ISmsGatewayIntegration, SmsGatewayMockIntegration>();

        return services;
    }
}


