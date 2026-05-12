using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations;

public sealed class SmsGatewayMockIntegration : ISmsGatewayIntegration
{
    public Task<BillingProvisionResult> SendAsync(string phoneNumber, string body, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new BillingProvisionResult(true, $"SMS mock → {phoneNumber}"));
    }
}
