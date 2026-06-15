using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations;

public sealed class MnpPortabilityMockGateway : IMnpPortabilityGateway
{
    public Task<MnpPortInOrderResult> SubmitPortInOrderAsync(
        MnpPortInOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var externalId = $"MNP-MOCK-{Guid.NewGuid():N}"[..24];
        return Task.FromResult(new MnpPortInOrderResult(
            true,
            "تم تسجيل طلب نقل الرقم (محاكاة MNP).",
            externalId));
    }

    public Task<MnpPortStatusPollResult> PollStatusAsync(
        MnpPortStatusPollRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MnpPortStatusPollResult(
            true,
            "PortInCompleted",
            "اكتمل نقل الرقم (محاكاة).",
            true));
    }
}
