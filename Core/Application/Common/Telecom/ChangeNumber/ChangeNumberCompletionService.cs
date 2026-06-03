using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.ChangeNumber;

public interface IChangeNumberCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? newMsisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class ChangeNumberCompletionService : IChangeNumberCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ICommandRepository<TelecomMsisdnChangeLog> _msisdnChangeLogRepository;
    private readonly ITelecomDirectorySync _directorySync;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeNumberCompletionService(
        IQueryContext query,
        ICommandRepository<TelecomOperationAuditLog> auditRepository,
        ICommandRepository<TelecomMsisdnChangeLog> msisdnChangeLogRepository,
        ITelecomDirectorySync directorySync,
        ISmsGatewayIntegration sms,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _auditRepository = auditRepository;
        _msisdnChangeLogRepository = msisdnChangeLogRepository;
        _directorySync = directorySync;
        _sms = sms;
        _unitOfWork = unitOfWork;
    }

    public async Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? newMsisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.NumberPortability)
        {
            return;
        }

        var priorMsisdn = await ResolveMsisdnAsync(operation.PriorMsisdnAssetId, cancellationToken);
        var targetMsisdn = newMsisdn ?? await ResolveMsisdnAsync(operation.TargetMsisdnAssetId, cancellationToken);

        var customerId = await _query.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == operation.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        var subscriptionId = await _query.TelecomSubscription.AsNoTracking()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfileId == operation.SubscriberProfileId
                        && s.MsisdnAssetId == operation.TargetMsisdnAssetId)
            .OrderByDescending(s => s.IsPrimaryLine)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(customerId)
            && !string.IsNullOrEmpty(operation.TargetMsisdnAssetId)
            && !string.IsNullOrEmpty(subscriptionId)
            && !string.IsNullOrEmpty(priorMsisdn)
            && !string.IsNullOrEmpty(targetMsisdn))
        {
            var syncResult = await _directorySync.NotifyMsisdnChangedAsync(
                new TelecomDirectoryMsisdnChangeRequest(
                    customerId,
                    operation.TargetMsisdnAssetId,
                    priorMsisdn,
                    targetMsisdn,
                    operation.CorrelationId),
                cancellationToken);

            await _msisdnChangeLogRepository.CreateAsync(new TelecomMsisdnChangeLog
            {
                CustomerId = customerId,
                SubscriberProfileId = operation.SubscriberProfileId,
                TelecomSubscriptionId = subscriptionId,
                MsisdnAssetId = operation.TargetMsisdnAssetId,
                OldMsisdn = priorMsisdn,
                NewMsisdn = targetMsisdn,
                ExternalSyncSuccess = syncResult.Success,
                ExternalSyncMessage = syncResult.Message,
                CreatedById = actorUserId,
            }, cancellationToken);
        }

        var snapshot = new
        {
            operation.Number,
            operation.CorrelationId,
            operation.NumberChangeReason,
            operation.PriorMsisdnAssetId,
            operation.TargetMsisdnAssetId,
            priorMsisdn,
            newMsisdn = targetMsisdn,
            operation.PremiumFeeAmount,
            completedAtUtc = DateTime.UtcNow,
        };

        await _auditRepository.CreateAsync(new TelecomOperationAuditLog
        {
            TelecomOperationRequestId = operation.Id,
            FromStatus = TelecomOperationStatus.Provisioning,
            ToStatus = TelecomOperationStatus.Completed,
            ActorUserId = actorUserId,
            Note = "CNR snapshot — تغيير رقم داخلي",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = operation.CorrelationId,
            FieldChangesJson = JsonSerializer.Serialize(snapshot),
            CreatedById = actorUserId,
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(targetMsisdn))
        {
            var body =
                $"سيريتل: تم تغيير رقم خطك إلى {targetMsisdn}. " +
                $"السبب: {operation.NumberChangeReason ?? "طلب مشترك"}. مرجع {operation.Number}.";
            await _sms.SendAsync(targetMsisdn, body, cancellationToken);
        }
    }

    private async Task<string?> ResolveMsisdnAsync(string? assetId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(assetId))
        {
            return null;
        }

        return await _query.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted && m.Id == assetId)
            .Select(m => m.Msisdn)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
