using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Telecom.Termination;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public sealed class TerminationCreateStrategy : IOperationCreateStrategy
{
    private readonly IPermissionEvaluator _permissions;
    private readonly ITerminationEligibilityChecker _eligibility;

    public TerminationCreateStrategy(
        IPermissionEvaluator permissions,
        ITerminationEligibilityChecker eligibility)
    {
        _permissions = permissions;
        _eligibility = eligibility;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.Termination;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        await OperationCreatePermissionHelpers.EnsureTerminationCreatePermissionAsync(
            context.ActorUserId,
            _permissions,
            cancellationToken);

        var result = await _eligibility.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId!,
            request.TerminationType!,
            request.TerminationReason!,
            request.RetentionOfferOutcome,
            excludeOperationId: null,
            cancellationToken: cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.TerminationEligibility = result;
    }

    public Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.TerminationEligibility;
        if (eligibility == null)
        {
            return Task.CompletedTask;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;

        entity.TerminationType = request.TerminationType!.Trim();
        entity.TerminationReason = request.TerminationReason!.Trim();
        entity.TerminationEffectiveDateUtc = request.TerminationEffectiveDateUtc ?? DateTime.UtcNow;
        entity.RetentionOfferOutcome = (request.RetentionOfferOutcome ?? string.Empty).Trim();
        entity.PriorMsisdnAssetId = eligibility.MsisdnAssetId ?? request.MsisdnAssetId;
        entity.PriorSimInventoryId = eligibility.PriorSimInventoryId;
        entity.DeprovisionStatus = "Pending";
        entity.PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? null
            : request.PaymentReference.Trim();
        entity.AgencyReference = string.IsNullOrWhiteSpace(request.AgencyReference)
            ? null
            : request.AgencyReference.Trim();
        entity.CollectionNote = string.IsNullOrWhiteSpace(request.CollectionNote)
            ? null
            : request.CollectionNote.Trim();
        if (!string.IsNullOrWhiteSpace(request.KycDocumentReferenceId))
        {
            entity.KycDocumentReferenceId = request.KycDocumentReferenceId.Trim();
            entity.DocumentStatus = TelecomDocumentStatus.Uploaded;
        }

        if (eligibility.RequiresBackOfficeApproval)
        {
            entity.ApprovalLevelRequired = "BackOffice";
        }

        entity.Notes = OperationCreateAuditHelpers.AppendTerminationAudit(entity.Notes, eligibility);
        return Task.CompletedTask;
    }
}
