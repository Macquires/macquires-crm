using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Features.TelecomManager.Events;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Common.Telecom;

public sealed class TelecomOperationOrchestrator : ITelecomOperationOrchestrator
{
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly IPublisher _publisher;
    private readonly IQueryContext _query;

    public TelecomOperationOrchestrator(
        ICommandRepository<TelecomOperationAuditLog> auditRepository,
        IPublisher publisher,
        IQueryContext query)
    {
        _auditRepository = auditRepository;
        _publisher = publisher;
        _query = query;
    }

    public async Task TransitionAsync(
        TelecomOperationRequest operation,
        TelecomOperationStatus toStatus,
        string? actorUserId,
        string? note,
        CancellationToken cancellationToken)
    {
        var from = operation.Status;
        if (from == toStatus) return;

        TelecomOperationLifecycle.EnsureCanTransition(from, toStatus);

        var audit = new TelecomOperationAuditLog
        {
            TelecomOperationRequestId = operation.Id,
            FromStatus = from,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            Note = note,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedById = actorUserId,
            ActivationChannel = operation.ActivationChannel,
            BranchId = operation.BranchId,
            DealerCode = operation.DealerCode,
            OverrideReasonCode = operation.OverrideReasonCode,
            CorrelationId = operation.CorrelationId,
            FieldChangesJson = JsonSerializer.Serialize(new
            {
                status = new { from = from.ToString(), to = toStatus.ToString() },
                paymentReference = operation.PaymentReference,
                documentStatus = operation.DocumentStatus.ToString()
            })
        };

        operation.Status = toStatus;
        operation.UpdatedById = actorUserId;
        operation.UpdatedAtUtc = DateTime.UtcNow;

        if (toStatus == TelecomOperationStatus.Confirmed)
        {
            operation.ConfirmedAtUtc ??= DateTime.UtcNow;
        }

        await _auditRepository.CreateAsync(audit, cancellationToken);

        var msisdn = await ResolveMsisdnAsync(operation, cancellationToken);
        await _publisher.Publish(
            new TelecomOperationStatusChangedNotification(
                operation.Id,
                operation.Kind,
                from,
                toStatus,
                operation.CorrelationId,
                msisdn,
                actorUserId),
            cancellationToken);
    }

    private async Task<string?> ResolveMsisdnAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            return await _query.MsisdnAsset.AsNoTracking()
                .Where(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }
}
