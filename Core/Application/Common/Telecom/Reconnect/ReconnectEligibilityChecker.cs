using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Settings;
using Application.Common.Telecom.Suspension;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Reconnect;

public sealed class ReconnectEligibilityChecker : IReconnectEligibilityChecker
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
    private readonly IGlobalSettingsProvider _settings;

    public ReconnectEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing, IGlobalSettingsProvider settings)
    {
        _query = query;
        _billing = billing;
        _settings = settings;
    }

    public async Task<ReconnectEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string reconnectReason,
        string clearanceType,
        string? paymentReference,
        bool fraudClearanceConfirmed,
        string? sourceSuspensionOperationId,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        var reason = (reconnectReason ?? string.Empty).Trim();
        var clearance = (clearanceType ?? string.Empty).Trim();

        if (!ReconnectWellKnown.IsKnownClearanceType(clearance))
        {
            throw new BusinessRuleViolationException(
                "نوع التسوية غير معروف (Payment, Fraud, Regulatory, Customer, Operational).");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleViolationException("سبب إعادة التفعيل مطلوب.");
        }

        var profileId = subscriberProfileId.Trim();
        var assetId = msisdnAssetId.Trim();

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        var asset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        var sourceOpId = await ResolveSourceSuspensionOperationIdAsync(
            assetId,
            sourceSuspensionOperationId,
            cancellationToken);

        var sourceOp = sourceOpId == null
            ? null
            : await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
                .Where(o => o.Id == sourceOpId)
                .Select(o => new { o.SuspensionType, o.SuspensionStartDateUtc, o.ConfirmedAtUtc, o.CreatedAtUtc })
                .FirstOrDefaultAsync(cancellationToken);

        var lastSuspensionType = sourceOp?.SuspensionType;
        var suspensionDate = sourceOp?.SuspensionStartDateUtc ?? sourceOp?.ConfirmedAtUtc ?? sourceOp?.CreatedAtUtc;

        var outstandingBalance = 0m;
        if (!string.IsNullOrEmpty(asset.Msisdn))
        {
            outstandingBalance = await _billing.GetOutstandingBalanceAsync(asset.Msisdn, cancellationToken);
        }

        var bdrStatus = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.Kind == TelecomOperationKind.BadDebtRecovery && o.MsisdnAssetId == assetId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => o.Status.ToString())
            .FirstOrDefaultAsync(cancellationToken);

        // DYNAMIC SYNC: Update static GlobalSettings from provider for the matrix to use
        GlobalSettings.PostpaidBadDebtThresholdMonths = await _settings.GetIntAsync(
            GlobalSettingKeys.TelecomPostpaidBadDebtThresholdMonths,
            GlobalSettings.PostpaidBadDebtThresholdMonths,
            cancellationToken: cancellationToken);

        var matrix = ReconnectEligibilityMatrix.Evaluate(new ReconnectEligibilityMatrixInput(
            profile.OperationalStatus,
            profile.Customer?.Status,
            asset.PoolStatus,
            lastSuspensionType,
            clearance,
            !string.IsNullOrWhiteSpace(paymentReference),
            outstandingBalance,
            fraudClearanceConfirmed,
            bdrStatus,
            suspensionDate));

        if (!matrix.Allowed)
        {
            return Deny(
                matrix.MessageAr,
                profile.CustomerId,
                asset.Msisdn,
                assetId,
                sourceOpId,
                matrix.RequiresBackOfficeApproval,
                matrix.ValidationCode);
        }

        await EnsureNoBlockingOperationAsync(assetId, excludeOperationId, cancellationToken);

        return new ReconnectEligibilityResult(
            true,
            matrix.MessageAr,
            profile.CustomerId,
            asset.Msisdn,
            assetId,
            sourceOpId,
            matrix.RequiresBackOfficeApproval,
            matrix.ValidationCode);
    }

    public async Task<ReconnectEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        var assetId = operation.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("رقم الخط غير محدد في الطلب.");

        return await ValidateForCreateAsync(
            operation.SubscriberProfileId,
            assetId,
            operation.ReconnectReason ?? string.Empty,
            operation.ClearanceType ?? ReconnectWellKnown.Customer,
            operation.PaymentReference,
            operation.FraudClearanceConfirmed,
            operation.SourceSuspensionOperationId,
            operation.Id,
            cancellationToken);
    }

    private async Task<string?> ResolveSourceSuspensionOperationIdAsync(
        string msisdnAssetId,
        string? explicitId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(explicitId))
        {
            return explicitId.Trim();
        }

        return await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.Kind == TelecomOperationKind.TemporarySuspension
                        && o.MsisdnAssetId == msisdnAssetId
                        && o.Status == TelecomOperationStatus.Completed)
            .OrderByDescending(o => o.ConfirmedAtUtc ?? o.CreatedAtUtc)
            .Select(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureNoBlockingOperationAsync(
        string msisdnAssetId,
        string? excludeOperationId,
        CancellationToken cancellationToken)
    {
        var query = _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.MsisdnAssetId == msisdnAssetId && BlockingStatuses.Contains(o.Status));

        if (!string.IsNullOrEmpty(excludeOperationId))
        {
            query = query.Where(o => o.Id != excludeOperationId);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessRuleViolationException(
                "VAL-09-01: يوجد عملية تليكوم مفتوحة على هذا الخط.");
        }
    }

    private static ReconnectEligibilityResult Deny(
        string messageAr,
        string? customerId,
        string? msisdn,
        string? msisdnAssetId,
        string? sourceSuspensionOperationId,
        bool requiresBo,
        string code) =>
        new(false, messageAr, customerId, msisdn, msisdnAssetId, sourceSuspensionOperationId, requiresBo, code);
}
