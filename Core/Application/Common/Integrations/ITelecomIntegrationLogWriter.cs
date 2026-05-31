using Domain.Enums;

namespace Application.Common.Integrations;

public interface ITelecomIntegrationLogWriter
{
    Task WriteAsync(
        TelecomIntegrationSystem system,
        string operationName,
        string? msisdn,
        string? requestPayload,
        string? responsePayload,
        bool isSuccess,
        string? responseStatusCode,
        long executionTimeMs,
        CancellationToken cancellationToken = default);
}
