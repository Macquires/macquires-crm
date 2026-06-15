using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Telecom.ChangeGsm;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationCreate;

public sealed class ChangeGsmCreateStrategy : IOperationCreateStrategy
{
    private readonly IChangeGsmEligibilityChecker _eligibility;
    private readonly IQueryContext _queryContext;

    public ChangeGsmCreateStrategy(
        IChangeGsmEligibilityChecker eligibility,
        IQueryContext queryContext)
    {
        _eligibility = eligibility;
        _queryContext = queryContext;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.ChangeGsmType;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        var result = await _eligibility.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId,
            request.TargetSubscriptionTypeId!,
            request.GsmMigrationReason,
            cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.ChangeGsmEligibility = result;
    }

    public async Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.ResolvedProductId) || context.ChangeGsmEligibility == null)
        {
            return;
        }

        await ValidateCatalogSelectionForChangeGsmAsync(
            context.ChangeGsmEligibility.SourceSubscriptionTypeId!,
            context.Request.TargetSubscriptionTypeId!,
            context.ResolvedProductId,
            context.ResolvedOfferingId,
            cancellationToken);
    }

    private async Task ValidateCatalogSelectionForChangeGsmAsync(
        string sourceTypeId,
        string targetTypeId,
        string resolvedProductId,
        string? resolvedOfferingId,
        CancellationToken cancellationToken)
    {
        var product = await _queryContext.Product
            .AsNoTracking()
            .FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == resolvedProductId, cancellationToken)
            ?? throw new BusinessRuleViolationException("المنتج التقني المرتبط غير موجود.");

        if (!string.IsNullOrEmpty(resolvedOfferingId))
        {
            var offering = await _queryContext.ProductOffering
                .AsNoTracking()
                .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == resolvedOfferingId, cancellationToken);

            if (offering != null
                && !string.IsNullOrEmpty(offering.CompatibleSubscriptionTypeId)
                && offering.CompatibleSubscriptionTypeId != targetTypeId)
            {
                throw new BusinessRuleViolationException(
                    "العرض التجاري المختار غير متوافق مع نوع الخط الهدف بعد التحويل.");
            }
        }

        if (!string.IsNullOrEmpty(product.CompatibleSubscriptionTypeId)
            && product.CompatibleSubscriptionTypeId != targetTypeId)
        {
            throw new BusinessRuleViolationException(
                "الباقة المختارة غير متوافقة مع نوع الخط الهدف (CGT).");
        }
    }

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.ChangeGsmEligibility;
        if (eligibility == null)
        {
            return Task.CompletedTask;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;

        entity.SourceSubscriptionTypeId = eligibility.SourceSubscriptionTypeId;
        entity.TargetSubscriptionTypeId = request.TargetSubscriptionTypeId!.Trim();
        entity.GsmMigrationReason = request.GsmMigrationReason!.Trim();
        entity.GsmEffectiveDateUtc = request.GsmEffectiveDateUtc ?? DateTime.UtcNow;
        entity.GsmCompatibilityStatus = eligibility.CompatibilityStatus;
        entity.PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? null
            : request.PaymentReference.Trim();
        entity.CollectionNote = string.IsNullOrWhiteSpace(request.CollectionNote)
            ? null
            : request.CollectionNote.Trim();
        if (!string.IsNullOrWhiteSpace(request.KycDocumentReferenceId))
        {
            entity.KycDocumentReferenceId = request.KycDocumentReferenceId.Trim();
            entity.DocumentStatus = TelecomDocumentStatus.Uploaded;
        }

        entity.Notes = OperationCreateAuditHelpers.AppendChangeGsmAudit(entity.Notes, eligibility);
        return Task.CompletedTask;
    }
}
