using Application.Common.Integrations;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

public sealed class ESimDpPlusMockService : IESimDpPlusService
{
    private readonly ILogger<ESimDpPlusMockService> _logger;

    public ESimDpPlusMockService(ILogger<ESimDpPlusMockService> logger) => _logger = logger;

    public Task<ESimActivationCodeResult> RequestActivationCodeAsync(
        ESimActivationCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SM-DP+ mock activation for EID {Eid} MSISDN {Msisdn}", request.Eid, request.Msisdn);
        var code = $"LPA:1$syriatel.mock$ACTIVATION-{Guid.CreateVersion7():N}"[..40];
        var qr = $"https://esim.syriatel.mock/qr?msisdn={Uri.EscapeDataString(request.Msisdn)}&code={Uri.EscapeDataString(code)}";
        return Task.FromResult(new ESimActivationCodeResult(true, code, qr, "SM-DP+ mock OK"));
    }
}
