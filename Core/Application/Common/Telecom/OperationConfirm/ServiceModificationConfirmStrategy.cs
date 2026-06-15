using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Telecom;
using Application.Common.Telecom.OfferSubscription;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationConfirm;

/// <summary>VAS / service modification — validates catalog eligibility and applies HLR + local state.</summary>
public sealed class ServiceModificationConfirmStrategy : IOperationConfirmStrategy
{
    private readonly IQueryContext _query;
    private readonly IOfferSubscriptionEligibilityChecker _eligibility;
    private readonly IVasOperationApplicator _vasApplicator;

    public ServiceModificationConfirmStrategy(
        IQueryContext query,
        IOfferSubscriptionEligibilityChecker eligibility,
        IVasOperationApplicator vasApplicator)
    {
        _query = query;
        _eligibility = eligibility;
        _vasApplicator = vasApplicator;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.ServiceModification;

    public bool RequiresNetworkProvision => false;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            return new(false, "رقم الخط غير محدد في طلب تعديل الخدمة.");
        }

        if (!VasOperationNotes.TryParse(entity.Notes, out var activate, out var serviceCode))
        {
            return new(false, "طلب تعديل الخدمة لا يحدد خدمة VAS (الصيغة: Activate VAS CODE أو Deactivate VAS CODE).");
        }

        var asset = await _query.MsisdnAsset
            .AsNoTracking()
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == entity.MsisdnAssetId, cancellationToken);
        if (asset == null)
        {
            return new(false, "رقم الخط غير موجود.");
        }

        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(asset.Msisdn ?? string.Empty);
        if (msisdn == null)
        {
            return new(false, "رقم الخط غير صالح.");
        }

        var check = await _eligibility.ValidateForVasToggleAsync(
            msisdn,
            serviceCode,
            activate,
            entity.Id,
            cancellationToken);
        if (!check.Allowed)
        {
            return new(false, check.MessageAr);
        }

        if (activate)
        {
            try
            {
                await _vasApplicator.ValidatePrepaidBalanceForActivateAsync(msisdn, serviceCode, cancellationToken);
            }
            catch (BusinessRuleViolationException ex)
            {
                return new(false, ex.Message);
            }
        }

        return new(true, "مسموح");
    }

    public async Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        await _vasApplicator.ApplyFromOperationAsync(entity, actorUserId, cancellationToken);
        return new OperationApplyResult(
            null,
            null,
            null,
            entity.SubscriberProfileId,
            null,
            RequiresBilling: false);
    }
}
