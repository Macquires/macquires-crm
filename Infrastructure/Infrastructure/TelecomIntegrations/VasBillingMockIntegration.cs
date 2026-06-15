using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations;

public sealed class VasBillingMockIntegration : IVasBillingIntegration
{
    public Task<VasBillingChargeResult> ChargeAsync(
        VasBillingChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            return Task.FromResult(new VasBillingChargeResult(false, "مبلغ رسوم VAS غير صالح."));
        }

        var chargeRef = $"VAS-JE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..28];
        return Task.FromResult(new VasBillingChargeResult(
            true,
            "تم خصم رسوم VAS (محاكاة CBS).",
            chargeRef));
    }
}
