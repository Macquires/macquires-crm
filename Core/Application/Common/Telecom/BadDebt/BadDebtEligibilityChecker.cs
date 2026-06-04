using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.BadDebt;

public sealed class BadDebtEligibilityChecker : IBadDebtEligibilityChecker
{
    private static readonly TelecomOperationStatus[] BlockingStatuses =
    [
        TelecomOperationStatus.Draft,
        TelecomOperationStatus.PendingDocuments,
        TelecomOperationStatus.Confirmed,
        TelecomOperationStatus.Provisioning,
        TelecomOperationStatus.PendingExternal
    ];

    private readonly IQueryContext _query;
    private readonly IBillingSystemIntegration _billing;

    public BadDebtEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<BadDebtEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string collectionAction,
        string? dunningStage,
        string? paymentReference,
        decimal? collectedAmount,
        decimal? writeOffAmount,
        bool collectionApprovalConfirmed,
        string? priorDunningStage = null,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        var action = (collectionAction ?? string.Empty).Trim();
        if (!BadDebtWellKnown.IsKnownAction(action))
        {
            throw new BusinessRuleViolationException(
                "إجراء التحصيل غير معروف (PaymentRecorded, PaymentPlan, DunningEscalation, AgencyReferral, WriteOffPartial, WriteOffFull).");
        }

        if (!BadDebtWellKnown.IsKnownStage(dunningStage))
        {
            throw new BusinessRuleViolationException("مرحلة التذكير (DunningStage) غير معروفة.");
        }

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == subscriberProfileId.Trim(), cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        var asset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == msisdnAssetId.Trim(), cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        var outstandingBalance = 0m;
        if (!string.IsNullOrEmpty(asset.Msisdn))
        {
            outstandingBalance = await _billing.GetOutstandingBalanceAsync(asset.Msisdn, cancellationToken);
        }

        var matrix = BadDebtEligibilityMatrix.Evaluate(new BadDebtEligibilityMatrixInput(
            profile.OperationalStatus,
            profile.Customer?.Status,
            action,
            dunningStage,
            priorDunningStage,
            !string.IsNullOrWhiteSpace(paymentReference),
            collectedAmount,
            writeOffAmount,
            outstandingBalance,
            collectionApprovalConfirmed));

        if (!matrix.Allowed)
        {
            return Deny(
                matrix.MessageAr,
                profile.CustomerId,
                asset.Msisdn,
                asset.Id,
                outstandingBalance,
                matrix.ValidationCode);
        }

        await EnsureNoBlockingBadDebtOperationAsync(asset.Id, excludeOperationId, cancellationToken);

        return new BadDebtEligibilityResult(
            true,
            matrix.MessageAr,
            profile.CustomerId,
            asset.Msisdn,
            asset.Id,
            outstandingBalance,
            matrix.RequiresBackOfficeApproval,
            matrix.ValidationCode);
    }

    public async Task<BadDebtEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        var assetId = operation.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("رقم الخط غير محدد في الطلب.");

        return await ValidateForCreateAsync(
            operation.SubscriberProfileId,
            assetId,
            operation.CollectionAction ?? string.Empty,
            operation.DunningStage,
            operation.PaymentReference,
            operation.CollectedAmount,
            operation.WriteOffAmount,
            operation.FraudClearanceConfirmed,
            operation.PriorDunningStage,
            operation.Id,
            cancellationToken);
    }

    private async Task EnsureNoBlockingBadDebtOperationAsync(
        string msisdnAssetId,
        string? excludeOperationId,
        CancellationToken cancellationToken)
    {
        var query = _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.Kind == TelecomOperationKind.BadDebtRecovery
                        && o.MsisdnAssetId == msisdnAssetId
                        && BlockingStatuses.Contains(o.Status));

        if (!string.IsNullOrEmpty(excludeOperationId))
        {
            query = query.Where(o => o.Id != excludeOperationId);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessRuleViolationException(
                "VAL-16-04: يوجد عملية تحصيل (BDR) مفتوحة على هذا الخط.");
        }
    }

    private static BadDebtEligibilityResult Deny(
        string messageAr,
        string? customerId,
        string? msisdn,
        string? msisdnAssetId,
        decimal balance,
        string code) =>
        new(false, messageAr, customerId, msisdn, msisdnAssetId, balance, false, code);
}
