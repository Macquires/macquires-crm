using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class OperationConfirmProvisionContextBuilder : IOperationConfirmProvisionContextBuilder
{
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;

    public OperationConfirmProvisionContextBuilder(
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<SubscriberProfile> profileRepository)
    {
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _subscriptionRepository = subscriptionRepository;
        _profileRepository = profileRepository;
    }

    public async Task<OperationProvisionContext> BuildAsync(
        TelecomOperationRequest entity,
        CancellationToken cancellationToken,
        string? iccidOverride = null)
    {
        string? msisdn = null;
        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            var asset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken);
            msisdn = asset?.Msisdn;
        }

        string? iccid = iccidOverride;
        if (string.IsNullOrEmpty(iccid) && !string.IsNullOrEmpty(entity.SimInventoryId))
        {
            var sim = await _simRepository.GetAsync(entity.SimInventoryId, cancellationToken);
            iccid = sim?.Iccid;
        }

        if (string.IsNullOrEmpty(iccid))
        {
            iccid = await _simRepository.GetQuery()
                .Where(s => !s.IsDeleted
                    && s.SubscriberProfileId == entity.SubscriberProfileId
                    && s.Status == SimStatus.Active)
                .Select(s => s.Iccid)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var sub = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == entity.SubscriberProfileId)
            .OrderByDescending(s => s.IsPrimaryLine)
            .FirstOrDefaultAsync(cancellationToken);

        var customerId = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        return new OperationProvisionContext(
            msisdn,
            iccid,
            customerId,
            entity.SubscriberProfileId,
            sub?.Id,
            entity.MsisdnAssetId,
            entity.SimInventoryId);
    }

    public async Task<OperationProvisionContext> BuildForMsisdnChangeAsync(
        TelecomOperationRequest entity,
        string? msisdn,
        string? priorMsisdn,
        CancellationToken cancellationToken)
    {
        string? iccid = null;
        if (!string.IsNullOrEmpty(entity.SimInventoryId))
        {
            var sim = await _simRepository.GetAsync(entity.SimInventoryId, cancellationToken);
            iccid = sim?.Iccid;
        }

        if (string.IsNullOrEmpty(iccid))
        {
            iccid = await _simRepository.GetQuery()
                .Where(s => !s.IsDeleted
                    && s.SubscriberProfileId == entity.SubscriberProfileId
                    && s.Status == SimStatus.Active)
                .Select(s => s.Iccid)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var sub = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == entity.SubscriberProfileId)
            .OrderByDescending(s => s.IsPrimaryLine)
            .FirstOrDefaultAsync(cancellationToken);

        var customerId = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        return new OperationProvisionContext(
            msisdn,
            iccid,
            customerId,
            entity.SubscriberProfileId,
            sub?.Id,
            entity.TargetMsisdnAssetId,
            entity.SimInventoryId);
    }
}
