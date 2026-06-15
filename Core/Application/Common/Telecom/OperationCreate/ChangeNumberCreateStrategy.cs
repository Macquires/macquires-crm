using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.ChangeNumber;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationCreate;

public sealed class ChangeNumberCreateStrategy : IOperationCreateStrategy
{
    private readonly IPermissionEvaluator _permissions;
    private readonly IChangeNumberEligibilityChecker _eligibility;
    private readonly IMnpPortabilityGateway _mnpGateway;
    private readonly IQueryContext _queryContext;

    public ChangeNumberCreateStrategy(
        IPermissionEvaluator permissions,
        IChangeNumberEligibilityChecker eligibility,
        IMnpPortabilityGateway mnpGateway,
        IQueryContext queryContext)
    {
        _permissions = permissions;
        _eligibility = eligibility;
        _mnpGateway = mnpGateway;
        _queryContext = queryContext;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.NumberPortability;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        await OperationCreatePermissionHelpers.EnsureChangeNumberCreatePermissionAsync(
            context.ActorUserId,
            _permissions,
            cancellationToken);

        ChangeNumberEligibilityResult result;
        if (ChangeNumberWellKnown.IsPortInMode(request.NumberChangeMode))
        {
            result = await _eligibility.ValidateForPortInCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId!,
                request.PortInMsisdn!,
                request.DonorOperatorCode!,
                request.NumberChangeReason!,
                request.AgencyReference,
                excludeOperationId: null,
                cancellationToken);
        }
        else
        {
            result = await _eligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId!,
                request.TargetMsisdnAssetId!,
                request.NumberChangeReason!,
                request.PremiumFeeAmount,
                excludeOperationId: null,
                cancellationToken: cancellationToken);
        }

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.ChangeNumberEligibility = result;
    }

    public Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.ChangeNumberEligibility;
        if (eligibility == null)
        {
            return;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;
        var mode = ChangeNumberWellKnown.IsPortInMode(request.NumberChangeMode)
            ? ChangeNumberModes.PortIn
            : ChangeNumberModes.Internal;

        entity.NumberChangeMode = mode;
        entity.NumberChangeReason = request.NumberChangeReason!.Trim();
        entity.NumberChangeEffectiveDateUtc = request.NumberChangeEffectiveDateUtc ?? DateTime.UtcNow;
        entity.PriorMsisdnAssetId = eligibility.PriorMsisdnAssetId ?? request.MsisdnAssetId;

        if (mode == ChangeNumberModes.PortIn)
        {
            entity.PortInMsisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.PortInMsisdn!)
                ?? request.PortInMsisdn!.Trim();
            entity.DonorOperatorCode = (request.DonorOperatorCode ?? string.Empty).Trim().ToUpperInvariant();
            entity.AgencyReference = string.IsNullOrWhiteSpace(request.AgencyReference)
                ? null
                : request.AgencyReference.Trim();
            entity.ApprovalLevelRequired = "BackOffice";

            var currentMsisdn = eligibility.CurrentMsisdn
                ?? await ResolveMsisdnAsync(request.MsisdnAssetId!, cancellationToken);

            var mnpResult = await _mnpGateway.SubmitPortInOrderAsync(
                new MnpPortInOrderRequest(
                    entity.Id,
                    entity.Number,
                    currentMsisdn,
                    entity.PortInMsisdn,
                    entity.DonorOperatorCode,
                    entity.AgencyReference,
                    entity.CorrelationId),
                cancellationToken);

            if (!mnpResult.Success)
            {
                throw new BusinessRuleViolationException(mnpResult.Message ?? "فشل تقديم طلب نقل الرقم (MNP).");
            }

            entity.ExternalCorrelationId = mnpResult.ExternalCorrelationId;
            entity.Notes = OperationCreateAuditHelpers.AppendPortInAudit(entity.Notes, eligibility, entity);
            return;
        }

        entity.TargetMsisdnAssetId = eligibility.TargetMsisdnAssetId ?? request.TargetMsisdnAssetId;
        entity.PremiumFeeAmount = request.PremiumFeeAmount;
        entity.PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? null
            : request.PaymentReference.Trim();
        if (eligibility.RequiresBackOfficeApproval)
        {
            entity.ApprovalLevelRequired = "BackOffice";
        }

        entity.Notes = OperationCreateAuditHelpers.AppendChangeNumberAudit(entity.Notes, eligibility);
    }

    private async Task<string> ResolveMsisdnAsync(string msisdnAssetId, CancellationToken cancellationToken)
    {
        return await _queryContext.MsisdnAsset.AsNoTracking()
                   .Where(m => !m.IsDeleted && m.Id == msisdnAssetId)
                   .Select(m => m.Msisdn)
                   .FirstOrDefaultAsync(cancellationToken)
               ?? string.Empty;
    }
}
