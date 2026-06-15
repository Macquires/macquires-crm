using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Settings;
using Application.Common.Telecom;
using Application.Common.Telecom.SellingLine;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationCreate;

public sealed class NewActivationCreateStrategy : IOperationCreateStrategy
{
    private readonly IGlobalSettingsProvider _globalSettings;
    private readonly IQueryContext _queryContext;
    private readonly IKycDocumentStorageService _kycDocumentStorage;
    private readonly ISellingLineEligibilityChecker _sellingLineEligibility;

    public NewActivationCreateStrategy(
        IGlobalSettingsProvider globalSettings,
        IQueryContext queryContext,
        IKycDocumentStorageService kycDocumentStorage,
        ISellingLineEligibilityChecker sellingLineEligibility)
    {
        _globalSettings = globalSettings;
        _queryContext = queryContext;
        _kycDocumentStorage = kycDocumentStorage;
        _sellingLineEligibility = sellingLineEligibility;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.NewActivation;

    public OperationCreatePostCreateFlags PostCreateFlags => OperationCreatePostCreateFlags.None;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        await ValidateNewActivationLineCapAsync(context.Request, cancellationToken);
        ValidateNewActivationKycDocument(context.Request);
    }

    public async Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        if (!string.IsNullOrEmpty(context.ResolvedProductId))
        {
            await _sellingLineEligibility.ValidateCatalogForCreateAsync(
                request,
                context.ResolvedProductId,
                context.ResolvedOfferingId,
                cancellationToken);
            await _sellingLineEligibility.ValidateForCreateAsync(request, cancellationToken);
        }
        else
        {
            await _sellingLineEligibility.ValidateForCreateAsync(request, cancellationToken);
        }
    }

    private void ValidateNewActivationKycDocument(CreateTelecomOperationRequest request)
    {
        var referenceId = (request.KycDocumentReferenceId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(referenceId))
        {
            throw new BusinessRuleViolationException(TelecomUserMessages.ValAct12KycRequired);
        }

        if (!_kycDocumentStorage.DocumentExists(referenceId))
        {
            throw new BusinessRuleViolationException(TelecomUserMessages.ValAct12KycVaultMissing);
        }
    }

    private async Task ValidateNewActivationLineCapAsync(
        CreateTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var maxLines = await _globalSettings.GetIntAsync(
            GlobalSettingKeys.TelecomMaxActiveLinesPerIndividual,
            defaultValue: 5,
            min: 1,
            max: 20,
            cancellationToken);

        var customerId = await _queryContext.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == request.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(customerId))
        {
            return;
        }

        var isIndividual = await _queryContext.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Id == customerId)
            .Select(c => c.CustomerKind)
            .FirstOrDefaultAsync(cancellationToken) == CustomerKind.Individual;

        if (!isIndividual)
        {
            return;
        }

        var activeLineCount = await (
            from s in _queryContext.TelecomSubscription.AsNoTracking()
            join m in _queryContext.MsisdnAsset.AsNoTracking() on s.MsisdnAssetId equals m.Id
            join p in _queryContext.SubscriberProfile.AsNoTracking() on s.SubscriberProfileId equals p.Id
            where !s.IsDeleted && !m.IsDeleted && p.CustomerId == customerId
                  && m.PoolStatus == MsisdnPoolStatus.Active
            select s.Id
        ).CountAsync(cancellationToken);

        if (activeLineCount >= maxLines)
        {
            throw new BusinessRuleViolationException(
                $"تجاوز الحد التنظيمي للخطوط النشطة ({maxLines}) لهذا العميل.");
        }
    }

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var request = build.Validation.Request;
        var entity = build.Entity;

        if (!string.IsNullOrWhiteSpace(request.TargetSubscriptionTypeId))
        {
            entity.TargetSubscriptionTypeId = request.TargetSubscriptionTypeId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.KycDocumentReferenceId))
        {
            entity.KycDocumentReferenceId = request.KycDocumentReferenceId.Trim();
            entity.DocumentStatus = TelecomDocumentStatus.Uploaded;
        }

        entity.ActivationEffectiveDateUtc = request.ActivationEffectiveDateUtc ?? DateTime.UtcNow;

        return Task.CompletedTask;
    }
}
