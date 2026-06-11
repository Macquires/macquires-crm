using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.TelecomIntegrations.Outbox;

public sealed class IntegrationOutboxService : IIntegrationOutbox
{
    private readonly ICommandRepository<IntegrationOutboxMessage> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public IntegrationOutboxService(
        ICommandRepository<IntegrationOutboxMessage> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task EnqueueAsync(
        string eventType,
        string payloadJson,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        await _repository.CreateAsync(new IntegrationOutboxMessage
        {
            EventType = eventType,
            PayloadJson = payloadJson,
            CorrelationId = correlationId,
            OccurredAtUtc = DateTime.UtcNow,
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
