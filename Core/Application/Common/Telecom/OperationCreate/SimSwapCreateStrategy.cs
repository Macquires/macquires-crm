using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Telecom.SimSwap;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public sealed class SimSwapCreateStrategy : IOperationCreateStrategy
{
    private readonly IPermissionEvaluator _permissions;
    private readonly ISimSwapEligibilityChecker _eligibility;

    public SimSwapCreateStrategy(
        IPermissionEvaluator permissions,
        ISimSwapEligibilityChecker eligibility)
    {
        _permissions = permissions;
        _eligibility = eligibility;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.SimSwap;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        await OperationCreatePermissionHelpers.EnsureSimSwapCreatePermissionAsync(
            context.ActorUserId,
            _permissions,
            cancellationToken);

        var result = await _eligibility.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId,
            string.IsNullOrEmpty((request.SimInventoryId ?? string.Empty).Trim()) ? null : request.SimInventoryId,
            request.SimIccid,
            request.ReplacementReason!,
            request.IsLostOrStolenReport,
            excludeOperationId: null,
            cancellationToken: cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.SimSwapEligibility = result;
    }

    public Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.SimSwapEligibility;
        if (eligibility == null)
        {
            return Task.CompletedTask;
        }

        if (!string.IsNullOrEmpty(eligibility.NewSimInventoryId))
        {
            build.SimInventoryId = eligibility.NewSimInventoryId;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;

        entity.ReplacementReason = request.ReplacementReason!.Trim();
        entity.SimSwapEffectiveDateUtc = request.SimSwapEffectiveDateUtc ?? DateTime.UtcNow;
        entity.IsLostOrStolenReport = request.IsLostOrStolenReport;
        entity.PriorSimInventoryId = eligibility.PriorSimInventoryId;
        if (request.IsLostOrStolenReport)
        {
            entity.ApprovalLevelRequired = "BackOffice";
            entity.Status = TelecomOperationStatus.PendingDocuments;
            entity.AgencyReference = string.IsNullOrWhiteSpace(request.AgencyReference)
                ? null
                : request.AgencyReference.Trim();
        }

        entity.Notes = OperationCreateAuditHelpers.AppendSimSwapAudit(entity.Notes, eligibility);
        return Task.CompletedTask;
    }
}
