using Application.Common.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.TelecomIntegrations.Http;

public sealed class MnpHttpGateway : IMnpPortabilityGateway
{
    private readonly ILogger<MnpHttpGateway> _logger;

    public MnpHttpGateway(ILogger<MnpHttpGateway> logger)
    {
        _logger = logger;
    }

    public async Task<MnpPortInOrderResult> SubmitPortInOrderAsync(
        MnpPortInOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MNP HTTP submit stub op={Op} port={PortIn} donor={Donor}",
            request.OperationNumber,
            request.PortInMsisdn,
            request.DonorOperatorCode);
        await Task.Delay(50, cancellationToken);
        var externalId = $"MNP-HTTP-{Guid.NewGuid():N}"[..24];
        return new MnpPortInOrderResult(true, "MNP HTTP stub submitted.", externalId);
    }

    public async Task<MnpPortStatusPollResult> PollStatusAsync(
        MnpPortStatusPollRequest request,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(25, cancellationToken);
        return new MnpPortStatusPollResult(true, "PortInCompleted", "MNP HTTP stub completed.", true);
    }
}
