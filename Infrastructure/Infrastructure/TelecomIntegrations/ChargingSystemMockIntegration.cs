using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations;

public sealed class ChargingSystemMockIntegration : IChargingSystemIntegration
{
    public Task<BillingProvisionResult> TouchAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new BillingProvisionResult(true, $"Charging mock OK ({correlationId})"));
    }
}
