using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.TakeOver;

public interface ITakeOverCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class TakeOverCompletionService : ITakeOverCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IUnitOfWork _unitOfWork;

    public TakeOverCompletionService(
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
        if (operation.Kind != TelecomOperationKind.TakeOver)
        {
            return;
        }

        var oldOwner = await ResolveCustomerLabelAsync(operation.OldCustomerId, cancellationToken);
        var newOwner = await ResolveCustomerLabelAsync(operation.NewCustomerId, cancellationToken);
        var priorProfileId = operation.PriorSubscriberProfileId ?? operation.SubscriberProfileId;
        var newProfileId = operation.SecondarySubscriberProfileId;

        var snapshot = new
        {
            operation.Number,
            operation.CorrelationId,
            priorProfileId,
            newProfileId,
            operation.OldCustomerId,
            operation.NewCustomerId,
            oldOwner,
            newOwner,
            operation.TransferReason,
            operation.DepositTransferPolicy,
            operation.TakeOverObligationStatus,
            operation.TakeOverEffectiveDateUtc,
            msisdn,
            completedAtUtc = DateTime.UtcNow,
        };

        await _auditRepository.CreateAsync(new TelecomOperationAuditLog
        {
            TelecomOperationRequestId = operation.Id,
            FromStatus = TelecomOperationStatus.Provisioning,
            ToStatus = TelecomOperationStatus.Completed,
            ActorUserId = actorUserId,
            Note = "TKO snapshot — الصندوق الأسود لنقل الملكية",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = operation.CorrelationId,
            FieldChangesJson = JsonSerializer.Serialize(snapshot),
            CreatedById = actorUserId,
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(msisdn))
        {
            var welcome =
                $"سيريتل: أصبح خطك {msisdn} باسمك. تم نقل الملكية بنجاح (مرجع {operation.Number}).";
            await _sms.SendAsync(msisdn, welcome, cancellationToken);
        }

        var oldMsisdn = await ResolvePriorOwnerMsisdnAsync(priorProfileId, operation.MsisdnAssetId, cancellationToken);
        if (!string.IsNullOrEmpty(oldMsisdn) && oldMsisdn != msisdn)
        {
            var release =
                $"سيريتل: تم نقل ملكية الخط {oldMsisdn} إلى مشترك آخر. لم يعد الخط مسجلاً باسمك (مرجع {operation.Number}).";
            await _sms.SendAsync(oldMsisdn, release, cancellationToken);
        }
    }

    private async Task<string?> ResolveCustomerLabelAsync(string? customerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(customerId))
        {
            return null;
        }

        return await _query.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Id == customerId)
            .Select(c => c.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> ResolvePriorOwnerMsisdnAsync(
        string? priorProfileId,
        string? msisdnAssetId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(msisdnAssetId))
        {
            return await _query.MsisdnAsset.AsNoTracking()
                .Where(m => !m.IsDeleted && m.Id == msisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(priorProfileId))
        {
            return null;
        }

        return await (
            from s in _query.TelecomSubscription.AsNoTracking()
            join m in _query.MsisdnAsset.AsNoTracking() on s.MsisdnAssetId equals m.Id
            where !s.IsDeleted && !m.IsDeleted && s.SubscriberProfileId == priorProfileId
            orderby s.IsPrimaryLine descending
            select m.Msisdn
        ).FirstOrDefaultAsync(cancellationToken);
    }
}
