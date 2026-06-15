using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations.Http;

public sealed class ESimHttpService : IESimDpPlusService
{
    private readonly ESimDpPlusMockService _fallback;

    public ESimHttpService(ESimDpPlusMockService fallback) => _fallback = fallback;

    public Task<ESimActivationCodeResult> RequestActivationCodeAsync(
        ESimActivationCodeRequest request,
        CancellationToken cancellationToken = default) =>
        _fallback.RequestActivationCodeAsync(request, cancellationToken);
}
