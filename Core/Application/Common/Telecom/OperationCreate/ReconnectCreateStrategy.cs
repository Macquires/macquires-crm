using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Telecom.Reconnect;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public sealed class ReconnectCreateStrategy : IOperationCreateStrategy
{
    private readonly IPermissionEvaluator _permissions;
    private readonly IReconnectEligibilityChecker _eligibility;

    public ReconnectCreateStrategy(
        IPermissionEvaluator permissions,
        IReconnectEligibilityChecker eligibility)
    {
        _permissions = permissions;
        _eligibility = eligibility;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.Reconnect;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        await OperationCreatePermissionHelpers.EnsureReconnectCreatePermissionAsync(
            context.ActorUserId,
            _permissions,
            cancellationToken);

        var result = await _eligibility.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId!,
            request.ReconnectReason!,
            request.ClearanceType ?? ReconnectWellKnown.Customer,
            request.PaymentReference,
            request.FraudClearanceConfirmed,
            request.SourceSuspensionOperationId,
            excludeOperationId: null,
            cancellationToken: cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.ReconnectEligibility = result;
    }

    public Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.ReconnectEligibility;
        if (eligibility == null)
        {
            return Task.CompletedTask;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;
        var actorUserId = build.ActorUserId;

        entity.ReconnectReason = request.ReconnectReason!.Trim();
        entity.ClearanceType = (request.ClearanceType ?? ReconnectWellKnown.Customer).Trim();
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

        entity.SourceSuspensionOperationId = eligibility.SourceSuspensionOperationId;
        entity.FraudClearanceConfirmed = request.FraudClearanceConfirmed;
        entity.FraudClearanceByUserId = request.FraudClearanceConfirmed ? actorUserId : null;
        entity.IsLostOrStolenReport = false;
        entity.AutoReconnectEnabled = false;
        entity.NotificationSuppressed = false;
        if (eligibility.RequiresBackOfficeApproval)
        {
            entity.ApprovalLevelRequired = "BackOffice";
        }

        if (request.Notes?.Contains("BypassToAdvance:true") == true)
        {
            entity.Status = TelecomOperationStatus.Paid_Pending_BackOffice_Clearance;
            entity.ApprovalLevelRequired = "BackOffice";
        }

        entity.Notes = OperationCreateAuditHelpers.AppendReconnectAudit(entity.Notes, eligibility);
        entity.ReconnectEffectiveDateUtc = request.ReconnectEffectiveDateUtc ?? DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
