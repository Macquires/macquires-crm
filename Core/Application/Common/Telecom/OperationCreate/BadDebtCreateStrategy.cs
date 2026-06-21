using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Settings;
using Application.Common.Telecom.BackOffice;
using Application.Common.Telecom.BadDebt;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public sealed class BadDebtCreateStrategy : IOperationCreateStrategy
{
    private readonly IPermissionEvaluator _permissions;
    private readonly IBadDebtEligibilityChecker _eligibility;
    private readonly IGlobalSettingsProvider _globalSettings;

    public BadDebtCreateStrategy(
        IPermissionEvaluator permissions,
        IBadDebtEligibilityChecker eligibility,
        IGlobalSettingsProvider globalSettings)
    {
        _permissions = permissions;
        _eligibility = eligibility;
        _globalSettings = globalSettings;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.BadDebtRecovery;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.EnqueueTechnicalTicket;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        await OperationCreatePermissionHelpers.EnsureBadDebtCreatePermissionAsync(
            context.ActorUserId,
            _permissions,
            cancellationToken);

        var result = await _eligibility.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId!,
            request.CollectionAction!,
            request.DunningStage,
            request.PaymentReference,
            request.CollectedAmount,
            request.WriteOffAmount,
            request.CollectionApprovalConfirmed,
            excludeOperationId: null,
            cancellationToken: cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.BadDebtEligibility = result;
    }

    public Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.BadDebtEligibility;
        if (eligibility == null)
        {
            return;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;

        entity.CollectionAction = request.CollectionAction!.Trim();
        entity.DunningStage = string.IsNullOrWhiteSpace(request.DunningStage)
            ? BadDebtWellKnown.Reminder1
            : request.DunningStage.Trim();
        entity.PriorDunningStage = entity.DunningStage;
        entity.OutstandingBalanceSnapshot = eligibility.OutstandingBalanceSnapshot;
        entity.CollectedAmount = request.CollectedAmount;
        entity.WriteOffAmount = request.WriteOffAmount;
        entity.AgencyReference = string.IsNullOrWhiteSpace(request.AgencyReference)
            ? null
            : request.AgencyReference.Trim();
        entity.PaymentPlanMonths = request.PaymentPlanMonths;
        entity.CollectionNote = string.IsNullOrWhiteSpace(request.CollectionNote)
            ? null
            : request.CollectionNote.Trim();
        entity.PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? null
            : request.PaymentReference.Trim();
        entity.FraudClearanceConfirmed = request.CollectionApprovalConfirmed;
        entity.CollectionSettlementStatus = BadDebtWellKnown.SettlementPending;
        entity.ProvisioningResult = "Pending";
        if (request.PaymentPlanMonths is > 0)
        {
            entity.NextDunningDueUtc = DateTime.UtcNow.AddMonths(request.PaymentPlanMonths.Value);
        }

        if (eligibility.RequiresBackOfficeApproval)
        {
            BackOfficeTelecomPipelineState.ApplyBackOfficeRouting(entity);

            var slaMinutes = await _globalSettings.GetIntAsync(
                GlobalSettingKeys.TelecomBdrTicketSlaMinutes,
                2,
                cancellationToken: cancellationToken);
            entity.SlaExpirationTimeUtc = DateTime.UtcNow.AddMinutes(slaMinutes);
        }

        entity.Notes = OperationCreateAuditHelpers.AppendBadDebtAudit(entity.Notes, eligibility);
        entity.BadDebtEffectiveDateUtc = request.BadDebtEffectiveDateUtc ?? DateTime.UtcNow;
    }
}
