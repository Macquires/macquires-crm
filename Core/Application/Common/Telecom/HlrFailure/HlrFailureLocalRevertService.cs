using Application.Common.Repositories;
using Application.Common.Telecom.Suspension;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.HlrFailure;

public interface IHlrFailureLocalRevertService
{
    Task<bool> RevertLocalBindAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken);
}

public sealed class HlrFailureLocalRevertService : IHlrFailureLocalRevertService
{
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ISubscriptionBindingCompensator _bindingCompensator;
    private readonly IUnitOfWork _unitOfWork;

    public HlrFailureLocalRevertService(
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ISubscriptionBindingCompensator bindingCompensator,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _profileRepository = profileRepository;
        _operationRepository = operationRepository;
        _bindingCompensator = bindingCompensator;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> RevertLocalBindAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (operation.Kind == TelecomOperationKind.ChangeGsmType
            && !string.IsNullOrEmpty(operation.SourceSubscriptionTypeId))
        {
            var subId = await _subscriptionRepository.GetQuery()
                .Where(s => !s.IsDeleted
                    && s.SubscriberProfileId == operation.SubscriberProfileId
                    && (string.IsNullOrEmpty(operation.MsisdnAssetId) || s.MsisdnAssetId == operation.MsisdnAssetId))
                .OrderByDescending(s => s.IsPrimaryLine)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrEmpty(subId))
            {
                return false;
            }

            var sub = await _subscriptionRepository.GetAsync(subId, cancellationToken);
            if (sub == null)
            {
                return false;
            }

            sub.SubscriptionTypeId = operation.SourceSubscriptionTypeId;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
            await _unitOfWork.SaveAsync(cancellationToken);
            return true;
        }

        return operation.Kind switch
        {
            TelecomOperationKind.TakeOver => await RevertTakeOverOwnershipAsync(operation, actorUserId, cancellationToken),
            TelecomOperationKind.SimSwap => await RevertSimSwapAsync(operation, actorUserId, cancellationToken),
            TelecomOperationKind.NumberPortability => await RevertChangeNumberAsync(operation, actorUserId, cancellationToken),
            TelecomOperationKind.Termination => await RevertTerminationAsync(operation, actorUserId, cancellationToken),
            TelecomOperationKind.Migration => await RevertMigrationAsync(operation, actorUserId, cancellationToken),
            TelecomOperationKind.TemporarySuspension => await RevertSuspensionAsync(operation, actorUserId, cancellationToken),
            TelecomOperationKind.Reconnect => await RevertReconnectAsync(operation, actorUserId, cancellationToken),
            TelecomOperationKind.NewActivation when !string.IsNullOrEmpty(operation.MsisdnAssetId)
                && !string.IsNullOrEmpty(operation.SimInventoryId) =>
                await RevertNewActivationBindAsync(operation, cancellationToken),
            _ => false,
        };
    }

    private async Task<bool> RevertNewActivationBindAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        var subId = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.MsisdnAssetId == operation.MsisdnAssetId
                && s.SubscriberProfileId == operation.SubscriberProfileId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(subId))
        {
            return false;
        }

        await _bindingCompensator.CompensateAsync(
            subId,
            operation.MsisdnAssetId!,
            operation.SimInventoryId!,
            operation.SubscriberProfileId,
            cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        return true;
    }

    private async Task<bool> RevertSimSwapAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var priorId = operation.PriorSimInventoryId;
        var newId = operation.SimInventoryId;
        if (string.IsNullOrEmpty(priorId) || string.IsNullOrEmpty(newId))
        {
            return false;
        }

        var changed = false;

        var newSim = await _simRepository.GetAsync(newId, cancellationToken);
        if (newSim != null && newSim.Status == SimStatus.Active)
        {
            newSim.TransitionTo(SimStatus.Quarantined);
            newSim.TransitionTo(SimStatus.Available);
            newSim.AssignToProfile(null);
            newSim.UpdatedById = actorUserId;
            _simRepository.Update(newSim);
            changed = true;
        }

        var priorSim = await _simRepository.GetAsync(priorId, cancellationToken);
        if (priorSim != null)
        {
            if (priorSim.Status == SimStatus.Quarantined)
            {
                priorSim.TransitionTo(SimStatus.Available);
            }

            if (priorSim.Status == SimStatus.Available)
            {
                priorSim.TransitionTo(SimStatus.Active);
            }

            priorSim.AssignToProfile(operation.SubscriberProfileId);
            priorSim.UpdatedById = actorUserId;
            _simRepository.Update(priorSim);
            changed = true;
        }

        if (changed)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return changed;
    }

    private async Task<bool> RevertChangeNumberAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var priorId = operation.PriorMsisdnAssetId ?? operation.MsisdnAssetId;
        var targetId = operation.TargetMsisdnAssetId;
        if (string.IsNullOrEmpty(priorId) || string.IsNullOrEmpty(targetId))
        {
            return false;
        }

        var changed = false;

        var subs = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfileId == operation.SubscriberProfileId
                        && s.MsisdnAssetId == targetId)
            .ToListAsync(cancellationToken);

        foreach (var sub in subs)
        {
            sub.MsisdnAssetId = priorId;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
            changed = true;
        }

        var targetAsset = await _msisdnRepository.GetAsync(targetId, cancellationToken);
        if (targetAsset != null)
        {
            if (targetAsset.PoolStatus == MsisdnPoolStatus.Active)
            {
                targetAsset.TransitionTo(MsisdnPoolStatus.Available);
            }

            targetAsset.SubscriberProfileId = null;
            targetAsset.UpdatedById = actorUserId;
            _msisdnRepository.Update(targetAsset);
            changed = true;
        }

        var priorAsset = await _msisdnRepository.GetAsync(priorId, cancellationToken);
        if (priorAsset != null)
        {
            if (priorAsset.PoolStatus == MsisdnPoolStatus.Quarantined)
            {
                priorAsset.TransitionTo(MsisdnPoolStatus.Active);
            }

            priorAsset.SubscriberProfileId = operation.SubscriberProfileId;
            priorAsset.UpdatedById = actorUserId;
            _msisdnRepository.Update(priorAsset);
            changed = true;
        }

        if (changed)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return changed;
    }

    private async Task<bool> RevertTerminationAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var msisdnId = operation.PriorMsisdnAssetId ?? operation.MsisdnAssetId;
        if (string.IsNullOrEmpty(msisdnId))
        {
            return false;
        }

        var changed = false;

        var profile = await _profileRepository.GetAsync(operation.SubscriberProfileId, cancellationToken);
        if (profile != null && profile.OperationalStatus == SubscriberOperationalStatus.Terminated)
        {
            profile.Activate();
            profile.UpdatedById = actorUserId;
            _profileRepository.Update(profile);
            changed = true;
        }

        var asset = await _msisdnRepository.GetAsync(msisdnId, cancellationToken);
        if (asset != null)
        {
            if (asset.PoolStatus == MsisdnPoolStatus.Quarantined)
            {
                asset.TransitionTo(MsisdnPoolStatus.Active);
            }

            asset.SubscriberProfileId = operation.SubscriberProfileId;
            asset.UpdatedById = actorUserId;
            _msisdnRepository.Update(asset);
            changed = true;
        }

        var quarantinedSims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                        && s.Status == SimStatus.Quarantined
                        && (s.SubscriberProfileId == null || s.SubscriberProfileId == operation.SubscriberProfileId))
            .ToListAsync(cancellationToken);

        foreach (var sim in quarantinedSims)
        {
            if (sim.Status == SimStatus.Quarantined)
            {
                sim.TransitionTo(SimStatus.Available);
            }

            if (sim.Status == SimStatus.Available)
            {
                sim.TransitionTo(SimStatus.Active);
            }

            sim.AssignToProfile(operation.SubscriberProfileId);
            sim.UpdatedById = actorUserId;
            _simRepository.Update(sim);
            changed = true;
        }

        var subs = await _subscriptionRepository.GetQuery()
            .Where(s => s.IsDeleted
                        && s.SubscriberProfileId == operation.SubscriberProfileId
                        && s.MsisdnAssetId == msisdnId)
            .ToListAsync(cancellationToken);

        foreach (var sub in subs)
        {
            sub.IsDeleted = false;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
            changed = true;
        }

        operation.DeprovisionStatus = null;
        operation.FinalBillAmount = null;

        if (changed)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return changed;
    }

    private async Task<bool> RevertMigrationAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            return false;
        }

        var sub = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfileId == operation.SubscriberProfileId
                        && s.MsisdnAssetId == operation.MsisdnAssetId)
            .OrderByDescending(s => s.IsPrimaryLine)
            .FirstOrDefaultAsync(cancellationToken);

        if (sub == null)
        {
            return false;
        }

        sub.ProductId = operation.PriorProductId;
        sub.ProductOfferingId = operation.PriorProductOfferingId;
        sub.UpdatedById = actorUserId;
        _subscriptionRepository.Update(sub);
        await _unitOfWork.SaveAsync(cancellationToken);
        return true;
    }

    private async Task<bool> RevertTakeOverOwnershipAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var priorProfileId = operation.PriorSubscriberProfileId ?? operation.SubscriberProfileId;
        var newProfileId = operation.SecondarySubscriberProfileId;
        if (string.IsNullOrEmpty(priorProfileId) || string.IsNullOrEmpty(newProfileId))
        {
            return false;
        }

        var changed = false;

        if (!string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            var asset = await _msisdnRepository.GetAsync(operation.MsisdnAssetId, cancellationToken);
            if (asset != null && asset.SubscriberProfileId == newProfileId)
            {
                asset.SubscriberProfileId = priorProfileId;
                asset.ReservedForCustomerId = operation.OldCustomerId;
                asset.UpdatedById = actorUserId;
                _msisdnRepository.Update(asset);
                changed = true;
            }
        }

        var subs = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == newProfileId
                && (string.IsNullOrEmpty(operation.MsisdnAssetId) || s.MsisdnAssetId == operation.MsisdnAssetId))
            .ToListAsync(cancellationToken);

        foreach (var sub in subs)
        {
            sub.SubscriberProfileId = priorProfileId;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
            changed = true;
        }

        var sims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == newProfileId
                && s.Status == SimStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var sim in sims)
        {
            sim.AssignToProfile(priorProfileId);
            sim.UpdatedById = actorUserId;
            _simRepository.Update(sim);
            changed = true;
        }

        if (changed)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return changed;
    }

    private async Task<bool> RevertSuspensionAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var msisdnId = operation.MsisdnAssetId;
        if (string.IsNullOrEmpty(msisdnId))
        {
            return false;
        }

        var changed = false;
        var profile = await _profileRepository.GetAsync(operation.SubscriberProfileId, cancellationToken);
        if (profile != null)
        {
            RestoreOperationalStatus(profile, operation.PriorOperationalStatus);
            profile.UpdatedById = actorUserId;
            _profileRepository.Update(profile);
            changed = true;
        }

        var asset = await _msisdnRepository.GetAsync(msisdnId, cancellationToken);
        if (asset != null && asset.PoolStatus == MsisdnPoolStatus.Suspended)
        {
            asset.TransitionTo(MsisdnPoolStatus.Active);
            asset.UpdatedById = actorUserId;
            _msisdnRepository.Update(asset);
            changed = true;
        }

        operation.BarStatus = null;

        if (changed)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return changed;
    }

    private async Task<bool> RevertReconnectAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var msisdnId = operation.MsisdnAssetId;
        if (string.IsNullOrEmpty(msisdnId))
        {
            return false;
        }

        var changed = false;
        var assetForMsisdn = await _msisdnRepository.GetAsync(msisdnId, cancellationToken);
        var msisdn = assetForMsisdn?.Msisdn ?? "";
        var profile = await _profileRepository.GetAsync(operation.SubscriberProfileId, cancellationToken);
        if (profile != null && profile.OperationalStatus == SubscriberOperationalStatus.Active)
        {
            var barring = await ResolveBarringLevelForReconnectRollbackAsync(operation, cancellationToken);
            ApplyBarringToProfile(profile, barring, msisdn);
            profile.UpdatedById = actorUserId;
            _profileRepository.Update(profile);
            changed = true;
        }

        var asset = await _msisdnRepository.GetAsync(msisdnId, cancellationToken);
        if (asset != null && asset.PoolStatus == MsisdnPoolStatus.Active)
        {
            asset.TransitionTo(MsisdnPoolStatus.Suspended);
            asset.UpdatedById = actorUserId;
            _msisdnRepository.Update(asset);
            changed = true;
        }

        operation.ProvisioningResult = null;
        operation.ReactivationAtUtc = null;

        if (changed)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return changed;
    }

    private async Task<string> ResolveBarringLevelForReconnectRollbackAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(operation.SourceSuspensionOperationId))
        {
            var source = await _operationRepository.GetAsync(operation.SourceSuspensionOperationId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(source?.BarringLevel))
            {
                return source.BarringLevel;
            }
        }

        return SuspensionWellKnown.BarringFull;
    }

    private static void RestoreOperationalStatus(SubscriberProfile profile, string? priorStatusName)
    {
        if (string.IsNullOrWhiteSpace(priorStatusName))
        {
            profile.Activate();
            return;
        }

        if (Enum.TryParse<SubscriberOperationalStatus>(priorStatusName, ignoreCase: true, out var prior))
        {
            profile.OperationalStatus = prior;
            return;
        }

        profile.Activate();
    }

    private static void ApplyBarringToProfile(SubscriberProfile profile, string barringLevel, string msisdn)
    {
        if (string.Equals(barringLevel, SuspensionWellKnown.BarringInboundOnly, StringComparison.OrdinalIgnoreCase))
        {
            profile.SuspendInbound(msisdn);
        }
        else if (string.Equals(barringLevel, SuspensionWellKnown.BarringOutboundOnly, StringComparison.OrdinalIgnoreCase))
        {
            profile.SuspendOutbound(msisdn);
        }
        else if (string.Equals(barringLevel, SuspensionWellKnown.BarringDataOnly, StringComparison.OrdinalIgnoreCase))
        {
            // Data-only bar — subscriber profile stays active for voice.
        }
        else
        {
            profile.Suspend(msisdn);
        }
    }
}
