using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Reconnect;

public interface IReconnectCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class ReconnectCompletionService : IReconnectCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IUnitOfWork _unitOfWork;

    public ReconnectCompletionService(
        IQueryContext query,
        ICommandRepository<TelecomOperationAuditLog> auditRepository,
        ISmsGatewayIntegration sms,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _auditRepository = auditRepository;
        _sms = sms;
        _unitOfWork = unitOfWork;
    }

    public async Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.Reconnect)
        {
            return;
        }

        operation.ProvisioningResult = "Completed";
        operation.ReactivationAtUtc ??= DateTime.UtcNow;

        var line = msisdn;
        if (string.IsNullOrEmpty(line) && !string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            line = await _query.MsisdnAsset.AsNoTracking()
                .Where(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var fieldChanges = System.Text.Json.JsonSerializer.Serialize(new
        {
            before = operation.PriorOperationalStatus,
            after = "Active",
            clearance = operation.ClearanceType,
            reason = operation.ReconnectReason,
            paymentRef = operation.PaymentReference,
            msisdn = line,
        });

        await _auditRepository.CreateAsync(
            new TelecomOperationAuditLog
            {
                TelecomOperationRequestId = operation.Id,
                FromStatus = TelecomOperationStatus.Provisioning,
                ToStatus = TelecomOperationStatus.Completed,
                ActorUserId = actorUserId,
                Note =
                    $"RCN completed|clearance={operation.ClearanceType}|reason={operation.ReconnectReason}|msisdn={line ?? "—"}",
                FieldChangesJson = fieldChanges,
                OccurredAtUtc = DateTime.UtcNow,
                CorrelationId = operation.CorrelationId,
                CreatedById = actorUserId,
            },
            cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(line))
        {
            var body =
                $"تم إعادة تفعيل خطك {line}. مرجع العملية: {operation.Number}.";
            await _sms.SendAsync(line, body, cancellationToken);
        }
    }
}
