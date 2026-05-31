namespace Application.Common.Integrations;

public sealed record VasProvisionRequest(
    string Msisdn,
    string HlrCommandTemplate,
    bool Activate,
    string? CorrelationId,
    string TelecomOperationRequestId);

public sealed record VasProvisionResult(bool Success, string Message, string? ExecutedCommand);

public interface IVasProvisioningService
{
    Task<VasProvisionResult> ProvisionVasAsync(VasProvisionRequest request, CancellationToken cancellationToken = default);
}
