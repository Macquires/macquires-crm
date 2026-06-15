using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Telecom;
using Application.Common.Telecom.OfferSubscription;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationCreate;

public sealed class ServiceModificationCreateStrategy : IOperationCreateStrategy
{
    private readonly IQueryContext _query;
    private readonly IOfferSubscriptionEligibilityChecker _eligibility;
    private readonly IVasOperationApplicator _vasApplicator;

    public ServiceModificationCreateStrategy(
        IQueryContext query,
        IOfferSubscriptionEligibilityChecker eligibility,
        IVasOperationApplicator vasApplicator)
    {
        _query = query;
        _eligibility = eligibility;
        _vasApplicator = vasApplicator;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.ServiceModification;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        if (string.IsNullOrEmpty(request.MsisdnAssetId))
        {
            throw new BusinessRuleViolationException("يجب تحديد خط المشترك لطلب تعديل الخدمة.");
        }

        if (!VasOperationNotes.TryParse(request.Notes, out var activate, out var serviceCode))
        {
            throw new BusinessRuleViolationException(
                "طلب تعديل الخدمة يتطلب تحديد خدمة VAS في الملاحظات (Activate VAS CODE أو Deactivate VAS CODE).");
        }

        var asset = await _query.MsisdnAsset
            .AsNoTracking()
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == request.MsisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(asset.Msisdn ?? string.Empty)
            ?? throw new BusinessRuleViolationException("رقم الخط غير صالح.");

        var result = await _eligibility.ValidateForVasToggleAsync(
            msisdn,
            serviceCode,
            activate,
            excludeOperationId: null,
            cancellationToken);
        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        if (activate)
        {
            await _vasApplicator.ValidatePrepaidBalanceForActivateAsync(msisdn, serviceCode, cancellationToken);
        }
    }

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
