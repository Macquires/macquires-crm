namespace Application.Common.Integrations;

public sealed record ESimActivationCodeRequest(string Eid, string Msisdn, string? CorrelationId);

public sealed record ESimActivationCodeResult(bool Success, string? ActivationCode, string? QrPayload, string Message);

/// <summary>SM-DP+ mock — fetches activation code / QR payload for eSIM.</summary>
public interface IESimDpPlusService
{
    Task<ESimActivationCodeResult> RequestActivationCodeAsync(ESimActivationCodeRequest request, CancellationToken cancellationToken = default);
}
