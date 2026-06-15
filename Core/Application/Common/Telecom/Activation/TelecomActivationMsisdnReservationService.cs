using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom.Inventory;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Activation;

public interface ITelecomActivationMsisdnReservationService
{
    Task EnsureReservedForCustomerAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken);
}

public sealed class TelecomActivationMsisdnReservationService : ITelecomActivationMsisdnReservationService
{
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ITelecomInventoryRulesProvider _inventoryRules;

    public TelecomActivationMsisdnReservationService(
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ITelecomInventoryRulesProvider inventoryRules)
    {
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _inventoryRules = inventoryRules;
    }

    public async Task EnsureReservedForCustomerAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (entity.Kind is TelecomOperationKind.Termination
            or TelecomOperationKind.TemporarySuspension
            or TelecomOperationKind.Reconnect
            or TelecomOperationKind.DeviceSale
            or TelecomOperationKind.DepositRefundSettlement
            or TelecomOperationKind.BadDebtRecovery)
        {
            return;
        }

        if (string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            return;
        }

        var customerId = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(customerId))
        {
            throw new BusinessRuleViolationException("ملف المشترك غير مرتبط بعميل.");
        }

        var asset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم MSISDN غير موجود.");

        var utcNow = DateTime.UtcNow;
        asset.ReleaseReservationIfExpired(utcNow);

        if (asset.PoolStatus == MsisdnPoolStatus.Available)
        {
            var reservationDuration = await _inventoryRules.GetMsisdnReservationDurationAsync(cancellationToken);
            asset.ReserveForCustomer(customerId, utcNow, reservationDuration);
            asset.UpdatedById = actorUserId;
            _msisdnRepository.Update(asset);
        }
        else if (asset.PoolStatus == MsisdnPoolStatus.Reserved && asset.ReservedForCustomerId != customerId)
        {
            throw new BusinessRuleViolationException("الرقم محجوز لعميل آخر.");
        }
    }
}
