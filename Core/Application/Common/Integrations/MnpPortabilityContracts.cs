namespace Application.Common.Integrations;

public sealed record MnpPortInOrderRequest(
    string OperationId,
    string OperationNumber,
    string CurrentMsisdn,
    string PortInMsisdn,
    string DonorOperatorCode,
    string? AgencyReference,
    string? CorrelationId = null);

public sealed record MnpPortInOrderResult(
    bool Success,
    string Message,
    string? ExternalCorrelationId = null);

public sealed record MnpPortStatusPollRequest(
    string ExternalCorrelationId,
    string? OperationId = null);

public sealed record MnpPortStatusPollResult(
    bool Success,
    string Status,
    string Message,
    bool IsTerminal);

/// <summary>MNP Port-In gateway — submit donor orders and poll completion.</summary>
public interface IMnpPortabilityGateway
{
    Task<MnpPortInOrderResult> SubmitPortInOrderAsync(
        MnpPortInOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<MnpPortStatusPollResult> PollStatusAsync(
        MnpPortStatusPollRequest request,
        CancellationToken cancellationToken = default);
}
