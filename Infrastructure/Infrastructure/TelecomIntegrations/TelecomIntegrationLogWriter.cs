using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

public sealed class TelecomIntegrationLogWriter : ITelecomIntegrationLogWriter
{
    private readonly ICommandRepository<TelecomIntegrationLog> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TelecomIntegrationLogWriter> _logger;

    public TelecomIntegrationLogWriter(
        ICommandRepository<TelecomIntegrationLog> repository,
        IUnitOfWork unitOfWork,
        ILogger<TelecomIntegrationLogWriter> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task WriteAsync(
        TelecomIntegrationSystem system,
        string operationName,
        string? msisdn,
        string? requestPayload,
        string? responsePayload,
        bool isSuccess,
        string? responseStatusCode,
        long executionTimeMs,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _repository.CreateAsync(new TelecomIntegrationLog
            {
                IntegrationSystem = system,
                OperationName = operationName,
                Msisdn = msisdn,
                RequestPayload = Truncate(requestPayload),
                ResponsePayload = Truncate(responsePayload),
                IsSuccess = isSuccess,
                ResponseStatusCode = Truncate(responseStatusCode, 64),
                ExecutionTimeMs = executionTimeMs,
                OccurredAtUtc = DateTime.UtcNow,
            }, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist TelecomIntegrationLog for {Operation}", operationName);
        }
    }

    private static string? Truncate(string? value, int max = 4000) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];
}
