using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom;

/// <summary>Reverses local triple-bind when CBS provisioning fails after commit.</summary>
public interface ISubscriptionBindingCompensator
{
    Task CompensateAsync(
        string telecomSubscriptionId,
        string msisdnAssetId,
        string simInventoryId,
        string subscriberProfileId,
        CancellationToken cancellationToken);
}

public sealed class SubscriptionBindingCompensator : ISubscriptionBindingCompensator
{
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;

    public SubscriptionBindingCompensator(
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<SubscriberProfile> profileRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _profileRepository = profileRepository;
    }

    public async Task CompensateAsync(
        string telecomSubscriptionId,
        string msisdnAssetId,
        string simInventoryId,
        string subscriberProfileId,
        CancellationToken cancellationToken)
    {
        var sub = await _subscriptionRepository.GetAsync(telecomSubscriptionId, cancellationToken);
        if (sub != null)
        {
            sub.IsDeleted = true;
            _subscriptionRepository.Update(sub);
        }

        var utcNow = DateTime.UtcNow;

        var msisdn = await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken);
        if (msisdn != null)
        {
            msisdn.RollbackFailedActivationBinding(utcNow);
            _msisdnRepository.Update(msisdn);
        }

        var sim = await _simRepository.GetAsync(simInventoryId, cancellationToken);
        if (sim != null)
        {
            sim.RollbackFailedActivationBinding(utcNow);
            _simRepository.Update(sim);
        }

        var profile = await _profileRepository.GetAsync(subscriberProfileId, cancellationToken);
        if (profile != null && profile.OperationalStatus == SubscriberOperationalStatus.Active)
        {
            profile.OperationalStatus = SubscriberOperationalStatus.Pending;
            _profileRepository.Update(profile);
        }
    }
}
