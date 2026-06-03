using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Suspension;

public interface ISuspensionCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class SuspensionCompletionService : ISuspensionCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IUnitOfWork _unitOfWork;

    public SuspensionCompletionService(
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
        if (operation.Kind != TelecomOperationKind.TemporarySuspension)
        {
            return;
        }

        operation.BarStatus = "Completed";
        operation.ProvisioningResult ??= "Completed";

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
            after = operation.BarringLevel,
            type = operation.SuspensionType,
            reason = operation.SuspensionReason,
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
                    $"SUS completed|type={operation.SuspensionType}|barring={operation.BarringLevel}|reason={operation.SuspensionReason}|msisdn={line ?? "—"}",
                FieldChangesJson = fieldChanges,
                OccurredAtUtc = DateTime.UtcNow,
                CorrelationId = operation.CorrelationId,
                CreatedById = actorUserId,
            },
            cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!operation.NotificationSuppressed && !string.IsNullOrEmpty(line))
        {
            var body =
                $"تم حظر خطك {line} مؤقتاً ({operation.SuspensionType}). مستوى الحظر: {operation.BarringLevel}. مرجع العملية: {operation.Number}.";
            await _sms.SendAsync(line, body, cancellationToken);
        }
    }
}
