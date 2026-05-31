using System.Diagnostics;
using Application.Common.Integrations;
using Domain.Enums;
using Infrastructure.Settings;

namespace Infrastructure.TelecomIntegrations;

public sealed class SmsGatewayMockIntegration : ISmsGatewayIntegration
{
    private readonly IntegrationEnablement _integrations;
    private readonly ITelecomIntegrationLogWriter _integrationLog;

    public SmsGatewayMockIntegration(IntegrationEnablement integrations, ITelecomIntegrationLogWriter integrationLog)
    {
        _integrations = integrations;
        _integrationLog = integrationLog;
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
