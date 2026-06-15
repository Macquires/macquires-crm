using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Telecom.OfferSubscription;
using Application.Features.ProductManager.Queries;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public sealed class MigrationCreateStrategy : IOperationCreateStrategy
{
    private readonly IOfferSubscriptionEligibilityChecker _offerSubscriptionEligibility;
    private readonly IQueryContext _queryContext;
    private readonly IBillingSystemIntegration _billing;

    public MigrationCreateStrategy(
        IOfferSubscriptionEligibilityChecker offerSubscriptionEligibility,
        IQueryContext queryContext,
        IBillingSystemIntegration billing)
    {
        _offerSubscriptionEligibility = offerSubscriptionEligibility;
        _queryContext = queryContext;
        _billing = billing;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.Migration;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        if (string.IsNullOrEmpty(context.ResolvedProductId) || string.IsNullOrEmpty(context.ResolvedOfferingId))
        {
            throw new BusinessRuleViolationException(
                "ترحيل الباقة يتطلب اختيار عرض تجاري مربوط بمنتج تقني (CBS).");
        }

        var result = await _offerSubscriptionEligibility.ValidateForMigrationCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId!,
            context.ResolvedOfferingId,
            context.ResolvedProductId,
            excludeOperationId: null,
            cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.MigrationEligibility = result;

        await EnsureMigrationProrationBalanceAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId!,
            context.ResolvedOfferingId,
            cancellationToken);
    }

    private async Task EnsureMigrationProrationBalanceAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string productOfferingId,
        CancellationToken cancellationToken)
    {
        var preview = await new GetMigrationProrationPreviewHandler(_queryContext, _billing).Handle(
            new GetMigrationProrationPreviewRequest
            {
                SubscriberProfileId = subscriberProfileId,
                MsisdnAssetId = msisdnAssetId,
                ProductOfferingId = productOfferingId,
            },
            cancellationToken);

        if (!preview.SufficientBalance)
        {
            throw new BusinessRuleViolationException(
                $"رصيد الخط غير كافٍ لتغطية رسوم الترحيل النسبية ({preview.ProratedAmount:N0} {preview.CurrencyCode} — الرصيد {preview.WalletBalance:N0}).");
        }
    }

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.MigrationEligibility;
        if (eligibility == null)
        {
            return Task.CompletedTask;
        }

        var entity = build.Entity;
        var request = build.Validation.Request;
        entity.PriorProductId = eligibility.PriorProductId;
        entity.PriorProductOfferingId = eligibility.PriorProductOfferingId;
        entity.MigrationEffectiveDateUtc = request.MigrationEffectiveDateUtc ?? DateTime.UtcNow;
        entity.Notes = OperationCreateAuditHelpers.AppendOfferMigrationAudit(entity.Notes, eligibility);
        return Task.CompletedTask;
    }
}
