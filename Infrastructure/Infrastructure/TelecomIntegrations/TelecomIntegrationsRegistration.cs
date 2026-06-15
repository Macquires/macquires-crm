using Infrastructure.TelecomIntegrations.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
namespace Infrastructure.TelecomIntegrations;

public static class TelecomIntegrationsRegistration
{
    public static IServiceCollection AddTelecomIntegrationMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<Messaging.RabbitMqOptions>(configuration.GetSection(Messaging.RabbitMqOptions.SectionName));

        var rabbitEnabled = configuration.GetValue($"{Messaging.RabbitMqOptions.SectionName}:Enabled", false);

        if (rabbitEnabled)
        {
            services.AddSingleton<Application.Common.Integrations.IIntegrationMessagePublisher, Messaging.RabbitMqIntegrationMessagePublisher>();
            services.AddHostedService<Messaging.RabbitMqIntegrationConsumerHostedService>();
        }
        else
        {
            services.AddScoped<Application.Common.Integrations.IIntegrationMessagePublisher, Messaging.InProcessIntegrationMessagePublisher>();
        }

        return services;
    }

    public static IServiceCollection AddTelecomHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TelecomHttpIntegrationOptions>(
            configuration.GetSection(TelecomHttpIntegrationOptions.SectionName));

        var options = configuration
            .GetSection(TelecomHttpIntegrationOptions.SectionName)
            .Get<TelecomHttpIntegrationOptions>() ?? new TelecomHttpIntegrationOptions();

        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>(static ex => !IsConnectionRefused(ex))
            .OrResult(static msg => (int)msg.StatusCode >= 500 || (int)msg.StatusCode == 429)
            .WaitAndRetryAsync(
                options.MaxRetryAttempts,
                attempt => TimeSpan.FromMilliseconds(200 * attempt));

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(options.TimeoutSeconds));

        void ConfigureClient(HttpClient client)
        {
            client.BaseAddress = new Uri(options.SimulatorBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds + 5);
        }

        services.AddHttpClient<SimulatorCbsHttpClient>(ConfigureClient)
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(timeoutPolicy);

        services.AddHttpClient<SimulatorHlrHttpClient>(ConfigureClient)
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(timeoutPolicy);

        services.AddHttpClient<SimulatorSmsHttpClient>(ConfigureClient)
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(timeoutPolicy);

        return services;
    }

    private static bool IsConnectionRefused(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.ConnectionRefused })
            {
                return true;
            }
        }

        return false;
    }
}
