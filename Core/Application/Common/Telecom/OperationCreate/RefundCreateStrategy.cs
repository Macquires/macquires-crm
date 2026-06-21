using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Telecom.BackOffice;
using Application.Common.Telecom.Refund;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public sealed class RefundCreateStrategy : IOperationCreateStrategy
{
    private readonly IPermissionEvaluator _permissions;
    private readonly IRefundEligibilityChecker _eligibility;

    public RefundCreateStrategy(
        IPermissionEvaluator permissions,
        IRefundEligibilityChecker eligibility)
    {
        _permissions = permissions;
        _eligibility = eligibility;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.DepositRefundSettlement;

    public OperationCreatePostCreateFlags PostCreateFlags => OperationCreatePostCreateFlags.None;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        await OperationCreatePermissionHelpers.EnsureRefundCreatePermissionAsync(
            context.ActorUserId,
            _permissions,
            cancellationToken);

        var result = await _eligibility.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId!,
            request.RefundType!,
            request.RefundMethod!,
            request.RefundAmount!.Value,
            request.RefundReason!,
            excludeOperationId: null,
            cancellationToken: cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.RefundEligibility = result;
    }

    public Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.RefundEligibility;
        if (eligibility == null)
        {
            return Task.CompletedTask;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;

        entity.RefundType = request.RefundType!.Trim();
        entity.RefundMethod = request.RefundMethod!.Trim();
        entity.RefundReason = request.RefundReason!.Trim();
        entity.RefundAmount = request.RefundAmount;
        entity.RefundCbsReference = string.IsNullOrWhiteSpace(request.RefundCbsReference)
            ? null
            : request.RefundCbsReference.Trim();
        entity.RefundGatewayReference = string.IsNullOrWhiteSpace(request.RefundGatewayReference)
            ? null
            : request.RefundGatewayReference.Trim();
        entity.DepositBalanceSnapshot = eligibility.DepositBalanceSnapshot;
        entity.WalletBalanceSnapshot = eligibility.WalletBalanceSnapshot;
        entity.RefundSettlementStatus = RefundWellKnown.SettlementPending;
        entity.RequiresDualApproval = eligibility.RequiresDualApproval;
        entity.ProvisioningResult = "Pending";
        if (eligibility.RequiresBackOfficeApproval)
        {
            BackOfficeTelecomPipelineState.ApplyBackOfficeRouting(entity);
        }

        entity.Notes = OperationCreateAuditHelpers.AppendRefundAudit(entity.Notes, eligibility);
        entity.RefundEffectiveDateUtc = request.RefundEffectiveDateUtc ?? DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
