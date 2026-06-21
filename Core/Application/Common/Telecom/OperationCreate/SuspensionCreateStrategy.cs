using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Telecom.BackOffice;
using Application.Common.Telecom.Suspension;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public sealed class SuspensionCreateStrategy : IOperationCreateStrategy
{
    private readonly IPermissionEvaluator _permissions;
    private readonly ISuspensionEligibilityChecker _eligibility;

    public SuspensionCreateStrategy(
        IPermissionEvaluator permissions,
        ISuspensionEligibilityChecker eligibility)
    {
        _permissions = permissions;
        _eligibility = eligibility;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.TemporarySuspension;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        await OperationCreatePermissionHelpers.EnsureSuspensionCreatePermissionAsync(
            context.ActorUserId,
            _permissions,
            cancellationToken);

        var result = await _eligibility.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId!,
            request.SuspensionType!,
            request.SuspensionReason!,
            request.BarringLevel ?? SuspensionWellKnown.BarringFull,
            request.AutoReconnectEnabled,
            request.SuspensionEndDateUtc,
            excludeOperationId: null,
            cancellationToken: cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.SuspensionEligibility = result;
    }

    public Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.SuspensionEligibility;
        if (eligibility == null)
        {
            return Task.CompletedTask;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;
        var actorUserId = build.ActorUserId;

        entity.SuspensionType = request.SuspensionType!.Trim();
        entity.SuspensionReason = request.SuspensionReason!.Trim();
        entity.BarringLevel = (request.BarringLevel ?? SuspensionWellKnown.BarringFull).Trim();
        entity.SuspensionStartDateUtc = request.SuspensionStartDateUtc ?? DateTime.UtcNow;
        entity.SuspensionEndDateUtc = request.SuspensionEndDateUtc;
        entity.AutoReconnectEnabled = request.AutoReconnectEnabled;
        entity.NotificationSuppressed = request.NotificationSuppressed;
        entity.BarStatus = "Pending";
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
            BackOfficeTelecomPipelineState.ApplyBackOfficeRouting(entity);
        }

        if (string.Equals(request.SuspensionType, SuspensionWellKnown.Fraud, StringComparison.OrdinalIgnoreCase))
        {
            entity.FraudClearanceConfirmed = request.FraudClearanceConfirmed;
            entity.FraudClearanceByUserId = request.FraudClearanceConfirmed ? actorUserId : null;
        }

        entity.Notes = OperationCreateAuditHelpers.AppendSuspensionAudit(entity.Notes, eligibility);
        return Task.CompletedTask;
    }
}
