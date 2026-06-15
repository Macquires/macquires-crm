using Application.Common.Integrations;
using Infrastructure.TelecomIntegrations.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Timeout;

namespace Infrastructure.TelecomIntegrations;

public static class TelecomIntegrationAdapterRegistration
{
    public static IServiceCollection AddTelecomIntegrationAdapters(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TelecomHttpIntegrationOptions>(
            configuration.GetSection(TelecomHttpIntegrationOptions.SectionName));

        var options = configuration
            .GetSection(TelecomHttpIntegrationOptions.SectionName)
            .Get<TelecomHttpIntegrationOptions>() ?? new TelecomHttpIntegrationOptions();

        RegisterModeScoped<IBillingPostingIntegration, BillingPostingMockIntegration, BillingPostingMockIntegration>(services, options, _ => false);
        RegisterModeScoped<IVasBillingIntegration, VasBillingMockIntegration, VasBillingMockIntegration>(services, options, _ => false);
        RegisterModeScoped<ITakeOverObligationSettlementIntegration, TakeOverObligationSettlementMockIntegration, TakeOverObligationSettlementMockIntegration>(services, options, _ => false);

        services.AddScoped<MnpPortabilityMockGateway>();
        services.AddHttpClient<MnpHttpGateway>(client =>
        {
            client.BaseAddress = new Uri(options.SimulatorBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds + 5);
        });
        services.AddScoped<IMnpPortabilityGateway>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TelecomHttpIntegrationOptions>>().Value;
            return TelecomIntegrationMode.IsHttp(opts, opts.Mnp)
                ? sp.GetRequiredService<MnpHttpGateway>()
                : sp.GetRequiredService<MnpPortabilityMockGateway>();
        });

        services.AddScoped<HuaweiIntelligentNetworkMockService>();
        services.AddScoped<IntelligentNetworkHttpService>();
        RegisterModeScoped<IIntelligentNetworkService, HuaweiIntelligentNetworkMockService, IntelligentNetworkHttpService>(
            services, options, o => TelecomIntegrationMode.IsHttp(o, o.IntelligentNetwork));

        services.AddScoped<CashierSystemMockIntegration>();
        services.AddScoped<CashierHttpIntegration>();
        RegisterModeScoped<IPosCashierIntegration, CashierSystemMockIntegration, CashierHttpIntegration>(
            services, options, o => TelecomIntegrationMode.IsHttp(o, o.Cashier));

        services.AddScoped<PaymentGatewayMockIntegration>();
        services.AddScoped<PaymentGatewayHttpIntegration>();
        RegisterModeScoped<IPaymentGatewayIntegration, PaymentGatewayMockIntegration, PaymentGatewayHttpIntegration>(
            services, options, o => TelecomIntegrationMode.IsHttp(o, o.PaymentGateway));

        services.AddScoped<DeviceInventoryMockIntegration>();
        services.AddScoped<DeviceInventoryHttpIntegration>();
        RegisterModeScoped<IDeviceInventoryIntegration, DeviceInventoryMockIntegration, DeviceInventoryHttpIntegration>(
            services, options, o => TelecomIntegrationMode.IsHttp(o, o.DeviceInventory));

        services.AddScoped<ESimDpPlusMockService>();
        services.AddScoped<ESimHttpService>();
        RegisterModeScoped<IESimDpPlusService, ESimDpPlusMockService, ESimHttpService>(
            services, options, o => TelecomIntegrationMode.IsHttp(o, o.ESim));

        return services;
    }

    private static void RegisterModeScoped<TService, TMock, THttp>(
        IServiceCollection services,
        TelecomHttpIntegrationOptions options,
        Func<TelecomHttpIntegrationOptions, bool> useHttp)
        where TService : class
        where TMock : class, TService
        where THttp : class, TService
    {
        services.AddScoped<TMock>();
        services.AddScoped<THttp>();
        services.AddScoped<TService>(sp =>
            useHttp(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TelecomHttpIntegrationOptions>>().Value)
                ? sp.GetRequiredService<THttp>()
                : sp.GetRequiredService<TMock>());
    }
}
