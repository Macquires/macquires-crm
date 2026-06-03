using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.ChangeGsm;

public interface IChangeGsmCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class ChangeGsmCompletionService : IChangeGsmCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeGsmCompletionService(
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
        if (operation.Kind != TelecomOperationKind.ChangeGsmType)
        {
            return;
        }

        var sourceLabel = await ResolveTypeLabelAsync(operation.SourceSubscriptionTypeId, cancellationToken);
        var targetLabel = await ResolveTypeLabelAsync(operation.TargetSubscriptionTypeId, cancellationToken);

        var snapshot = new
        {
            operation.SourceSubscriptionTypeId,
            operation.TargetSubscriptionTypeId,
            sourceLabel,
            targetLabel,
            operation.GsmMigrationReason,
            operation.GsmCompatibilityStatus,
            operation.CorrelationId,
            operation.GsmEffectiveDateUtc,
            completedAtUtc = DateTime.UtcNow,
        };

        await _auditRepository.CreateAsync(new TelecomOperationAuditLog
        {
            TelecomOperationRequestId = operation.Id,
            FromStatus = TelecomOperationStatus.Provisioning,
            ToStatus = TelecomOperationStatus.Completed,
            ActorUserId = actorUserId,
            Note = "CGT snapshot — نوع الخط قبل/بعد",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = operation.CorrelationId,
            FieldChangesJson = JsonSerializer.Serialize(snapshot),
            CreatedById = actorUserId,
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(msisdn))
        {
            var body =
                $"سيريتل: تم تحويل نوع خطك {msisdn} من {sourceLabel ?? "—"} إلى {targetLabel ?? "—"}. " +
                $"السبب: {operation.GsmMigrationReason ?? "طلب مشترك"}.";
            await _sms.SendAsync(msisdn, body, cancellationToken);
        }
    }

    private async Task<string?> ResolveTypeLabelAsync(string? typeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(typeId))
        {
            return null;
        }

        var row = await _query.TelecomSubscriptionTypeLookup.AsNoTracking()
            .Where(t => !t.IsDeleted && t.Id == typeId)
            .Select(t => new { t.NameAr, t.Code })
            .FirstOrDefaultAsync(cancellationToken);

        return row == null ? typeId : $"{row.NameAr} ({row.Code})";
    }
}
