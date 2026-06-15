using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom.TakeOver;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class TakeOverConfirmStrategy : IOperationConfirmStrategy
{
    private readonly ITakeOverEligibilityChecker _eligibility;
    private readonly ITakeOverObligationSettlementIntegration _obligationSettlement;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;

    public TakeOverConfirmStrategy(
        ITakeOverEligibilityChecker eligibility,
        ITakeOverObligationSettlementIntegration obligationSettlement,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<SimInventory> simRepository)
    {
        _eligibility = eligibility;
        _obligationSettlement = obligationSettlement;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _subscriptionRepository = subscriptionRepository;
        _simRepository = simRepository;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.TakeOver;

    public bool RequiresNetworkProvision => true;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entity.SecondarySubscriberProfileId))
        {
            return new(false, "يجب تحديد المالك الجديد قبل اعتماد نقل الملكية.");
        }

        if (entity.SecondarySubscriberProfileId == entity.SubscriberProfileId)
        {
            return new(false, "المالك الجديد يجب أن يختلف عن المالك الحالي.");
        }

        var check = await _eligibility.ValidateForConfirmAsync(entity, cancellationToken);
        if (!check.Allowed)
        {
            return new(check.Allowed, check.MessageAr);
        }

        if (!string.IsNullOrWhiteSpace(entity.TakeOverObligationStatus))
        {
            var msisdn = entity.MsisdnAsset?.Msisdn ?? string.Empty;
            var settlement = await _obligationSettlement.SettleAsync(
                new TakeOverObligationSettlementRequest(
                    entity.Id,
                    entity.Number,
                    msisdn,
                    entity.TakeOverObligationStatus,
                    entity.PaymentReference,
                    null,
                    entity.CorrelationId,
                    entity.BranchId),
                cancellationToken);
            if (!settlement.Success)
            {
                return new(false, settlement.Message);
            }
        }

        return new(check.Allowed, check.MessageAr);
    }

    public async Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        entity.PriorSubscriberProfileId ??= entity.SubscriberProfileId;

        if (!string.IsNullOrWhiteSpace(entity.TakeOverObligationStatus)
            && !string.Equals(entity.TakeOverObligationStatus, "Clear", StringComparison.OrdinalIgnoreCase))
        {
            var msisdnForSettlement = string.Empty;
            if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
            {
                var asset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken);
                msisdnForSettlement = asset?.Msisdn ?? string.Empty;
            }

            var settlement = await _obligationSettlement.SettleAsync(
                new TakeOverObligationSettlementRequest(
                    entity.Id,
                    entity.Number,
                    msisdnForSettlement,
                    entity.TakeOverObligationStatus,
                    entity.PaymentReference,
                    entity.InitialDepositAmount,
                    entity.CorrelationId,
                    entity.BranchId),
                cancellationToken);

            if (!settlement.Success)
            {
                throw new BusinessRuleViolationException(settlement.Message ?? "فشل تسوية ذمة المالك السابق.");
            }

            entity.TakeOverObligationStatus = "Settled";
            entity.PaymentReference ??= settlement.SettlementReference;
        }

        var newProfileId = entity.SecondarySubscriberProfileId
            ?? throw new BusinessRuleViolationException("يجب تحديد المالك الجديد.");

        if (newProfileId == entity.SubscriberProfileId)
        {
            throw new BusinessRuleViolationException("المالك الجديد يجب أن يختلف عن المالك الحالي.");
        }

        var newCustomerId = await _profileRepository.GetQuery()
            .Where(p => p.Id == newProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المالك الجديد غير موجود.");

        string? msisdn = null;
        string? iccid = null;
        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            var msisdnAsset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken)
                ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

            if (msisdnAsset.PoolStatus != MsisdnPoolStatus.Active)
            {
                throw new BusinessRuleViolationException(
                    $"لا يمكن نقل ملكية الرقم {msisdnAsset.Msisdn} لأن حالته {msisdnAsset.PoolStatus}.");
            }

            msisdn = msisdnAsset.Msisdn;
            msisdnAsset.SubscriberProfileId = newProfileId;
            msisdnAsset.ReservedForCustomerId = newCustomerId;
            msisdnAsset.UpdatedById = actorUserId;
            _msisdnRepository.Update(msisdnAsset);
        }

        var subsQuery = _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == entity.SubscriberProfileId);

        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            subsQuery = subsQuery.Where(s => s.MsisdnAssetId == entity.MsisdnAssetId);
        }

        var subs = await subsQuery.ToListAsync(cancellationToken);
        foreach (var sub in subs)
        {
            sub.SubscriberProfileId = newProfileId;
            sub.DocumentStatus = TelecomDocumentStatus.Verified;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
        }

        var activeSims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == entity.SubscriberProfileId
                && s.Status == SimStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var sim in activeSims)
        {
            sim.AssignToProfile(newProfileId);
            sim.UpdatedById = actorUserId;
            _simRepository.Update(sim);
        }

        entity.DocumentStatus = TelecomDocumentStatus.Verified;
        entity.UpdatedById = actorUserId;

        if (!string.IsNullOrEmpty(entity.SimInventoryId))
        {
            var sim = await _simRepository.GetAsync(entity.SimInventoryId, cancellationToken);
            iccid = sim?.Iccid;
        }

        if (string.IsNullOrEmpty(iccid) && activeSims.Count > 0)
        {
            iccid = activeSims[0].Iccid;
        }

        var primarySub = subs.FirstOrDefault(s => s.IsPrimaryLine) ?? subs.FirstOrDefault();
        return new OperationApplyResult(msisdn, iccid, newCustomerId, newProfileId, primarySub?.Id);
    }
}
