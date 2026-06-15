using System.Diagnostics;
using Application.Common.Integrations;
using Domain.Enums;
using Infrastructure.Settings;
using Infrastructure.TelecomIntegrations.Http;
using Microsoft.Extensions.Options;

namespace Infrastructure.TelecomIntegrations;

public sealed class SmsGatewayMockIntegration : ISmsGatewayIntegration
{
    private readonly IntegrationEnablement _integrations;
    private readonly ITelecomIntegrationLogWriter _integrationLog;
    private readonly IOptions<TelecomHttpIntegrationOptions> _httpOptions;
    private readonly SimulatorSmsHttpClient _smsHttp;

    public SmsGatewayMockIntegration(
        IntegrationEnablement integrations,
        ITelecomIntegrationLogWriter integrationLog,
        IOptions<TelecomHttpIntegrationOptions> httpOptions,
        SimulatorSmsHttpClient smsHttp)
    {
        _integrations = integrations;
        _integrationLog = integrationLog;
        _httpOptions = httpOptions;
        _smsHttp = smsHttp;
    }

    public async Task<BillingProvisionResult> SendAsync(string phoneNumber, string body, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        if (!await _integrations.IsSmsEnabledAsync(cancellationToken))
        {
            var payload = $"{IntegrationCircuitBreaker.FallbackNotesEn} | {IntegrationCircuitBreaker.FallbackMessageAr}";
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.SmsGateway,
                "SendSms",
                phoneNumber,
                body,
                payload,
                true,
                IntegrationCircuitBreaker.ResponseStatusFallback,
                sw.ElapsedMilliseconds,
                cancellationToken);
            return new BillingProvisionResult(true, IntegrationCircuitBreaker.FallbackMessageAr, QueuedForSync: true);
        }

        if (TelecomIntegrationMode.IsHttp(_httpOptions.Value))
        {
            var httpResult = await _smsHttp.SendAsync(phoneNumber, body, cancellationToken);
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.SmsGateway,
                "SendSms",
                phoneNumber,
                body,
                httpResult.Message,
                httpResult.Success,
                httpResult.Success ? "200" : "500",
                sw.ElapsedMilliseconds,
                cancellationToken);
            return httpResult;
        }

        await _integrationLog.WriteAsync(
            TelecomIntegrationSystem.SmsGateway,
            "SendSms",
            phoneNumber,
            body,
            $"SMS mock → {phoneNumber}",
            true,
            "200",
            sw.ElapsedMilliseconds,
            cancellationToken);

        return new BillingProvisionResult(true, $"SMS mock → {phoneNumber}");
    }
}
