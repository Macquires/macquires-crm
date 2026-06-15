using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.ChangeNumber;

public sealed class ChangeNumberEligibilityChecker : IChangeNumberEligibilityChecker
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

    public ChangeNumberEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<ChangeNumberEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string currentMsisdnAssetId,
        string targetMsisdnAssetId,
        string numberChangeReason,
        decimal? premiumFeeAmount,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numberChangeReason))
        {
            throw new BusinessRuleViolationException("سبب تغيير الرقم مطلوب.");
        }

        var profileId = subscriberProfileId.Trim();
        var currentId = currentMsisdnAssetId.Trim();
        var targetId = targetMsisdnAssetId.Trim();

        if (string.Equals(currentId, targetId, StringComparison.Ordinal))
        {
            return Deny(
                "VAL-05-04: الرقم المستهدف مطابق للرقم الحالي.",
                null,
                null,
                null,
                currentId,
                targetId,
                false,
                "SameNumber");
        }

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        EnsureCustomerEligible(profile.Customer);

        var currentAsset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == currentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط الحالي غير موجود.");

        if (currentAsset.PoolStatus != MsisdnPoolStatus.Active)
        {
            return Deny(
                $"VAL-05-04: لا يمكن تغيير الرقم — حالة الرقم الحالي {currentAsset.PoolStatus}.",
                profile.CustomerId,
                currentAsset.Msisdn,
                null,
                currentId,
                targetId,
                false,
                "CurrentNotActive");
        }

        if (!string.Equals(currentAsset.SubscriberProfileId, profileId, StringComparison.Ordinal))
        {
            return Deny(
                "VAL-05-04: الرقم الحالي غير مربوط بملف المشترك.",
                profile.CustomerId,
                currentAsset.Msisdn,
                null,
                currentId,
                targetId,
                false,
                "CurrentNotOwned");
        }

        var targetAsset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == targetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("الرقم المستهدف غير موجود في المخزون.");

        var targetCheck = ValidateTargetPoolState(targetAsset, profile.CustomerId);
        if (targetCheck != null)
        {
            return Deny(
                targetCheck,
                profile.CustomerId,
                currentAsset.Msisdn,
                targetAsset.Msisdn,
                currentId,
                targetId,
                false,
                "TargetInvalid");
        }

        if (!string.IsNullOrEmpty(currentAsset.Msisdn))
        {
            await EnsureNoOutstandingDebtAsync(currentAsset.Msisdn, cancellationToken);
        }

        await EnsureNoBlockingChangeNumberAsync(currentId, excludeOperationId, cancellationToken);

        var requiresBo = RequiresBackOfficeApproval(targetAsset, premiumFeeAmount, null);

        if (requiresBo && !ChangeNumberWellKnown.IsPremiumFeeSatisfied(targetAsset.Category, premiumFeeAmount, null))
        {
            return new ChangeNumberEligibilityResult(
                true,
                "VAL-05-02: الرقم مميز — بانتظار اعتماد الباك أوفيس أو تسجيل رسوم التميز.",
                profile.CustomerId,
                currentAsset.Msisdn,
                targetAsset.Msisdn,
                currentId,
                targetId,
                true,
                "PremiumPendingApproval");
        }

        return new ChangeNumberEligibilityResult(
            true,
            "تغيير الرقم مسموح.",
            profile.CustomerId,
            currentAsset.Msisdn,
            targetAsset.Msisdn,
            currentId,
            targetId,
            requiresBo,
            "Allowed");
    }

    public async Task<ChangeNumberEligibilityResult> ValidateForPortInCreateAsync(
        string subscriberProfileId,
        string currentMsisdnAssetId,
        string portInMsisdn,
        string donorOperatorCode,
        string numberChangeReason,
        string? portInReference,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numberChangeReason))
        {
            throw new BusinessRuleViolationException("سبب نقل الرقم مطلوب.");
        }

        if (string.IsNullOrWhiteSpace(portInReference))
        {
            throw new BusinessRuleViolationException("مرجع طلب النقل (MNP) مطلوب.");
        }

        var donor = (donorOperatorCode ?? string.Empty).Trim().ToUpperInvariant();
        if (!ChangeNumberWellKnown.IsValidDonorOperator(donor))
        {
            return Deny(
                "VAL-MNP-01: مشغّل المانح غير صالح أو يطابق المشغّل المحلي.",
                null,
                null,
                null,
                currentMsisdnAssetId.Trim(),
                null,
                true,
                "InvalidDonor");
        }

        var canonicalPortIn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(portInMsisdn)
            ?? throw new BusinessRuleViolationException("رقم النقل غير صالح.");

        var profileId = subscriberProfileId.Trim();
        var currentId = currentMsisdnAssetId.Trim();

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        EnsureCustomerEligible(profile.Customer);

        var currentAsset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == currentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط الحالي غير موجود.");

        if (currentAsset.PoolStatus != MsisdnPoolStatus.Active)
        {
            return Deny(
                $"VAL-MNP-02: لا يمكن نقل الرقم — حالة الخط الحالي {currentAsset.PoolStatus}.",
                profile.CustomerId,
                currentAsset.Msisdn,
                canonicalPortIn,
                currentId,
                null,
                true,
                "CurrentNotActive");
        }

        if (!string.Equals(currentAsset.SubscriberProfileId, profileId, StringComparison.Ordinal))
        {
            return Deny(
                "VAL-MNP-02: الخط الحالي غير مربوط بملف المشترك.",
                profile.CustomerId,
                currentAsset.Msisdn,
                canonicalPortIn,
                currentId,
                null,
                true,
                "CurrentNotOwned");
        }

        if (string.Equals(currentAsset.Msisdn, canonicalPortIn, StringComparison.Ordinal))
        {
            return Deny(
                "VAL-MNP-03: رقم النقل مطابق للرقم الحالي.",
                profile.CustomerId,
                currentAsset.Msisdn,
                canonicalPortIn,
                currentId,
                null,
                true,
                "SameNumber");
        }

        var portInActive = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo(false)
            .Include(s => s.MsisdnAsset)
            .AnyAsync(
                s => s.MsisdnAsset != null
                     && s.MsisdnAsset.Msisdn == canonicalPortIn
                     && s.MsisdnAsset.PoolStatus == MsisdnPoolStatus.Active,
                cancellationToken);
        if (portInActive)
        {
            return Deny(
                "VAL-MNP-04: رقم النقل نشط مسبقاً على الشبكة.",
                profile.CustomerId,
                currentAsset.Msisdn,
                canonicalPortIn,
                currentId,
                null,
                true,
                "PortInAlreadyActive");
        }

        if (!string.IsNullOrEmpty(currentAsset.Msisdn))
        {
            await EnsureNoOutstandingDebtAsync(currentAsset.Msisdn, cancellationToken);
        }

        await EnsureNoBlockingChangeNumberAsync(currentId, excludeOperationId, cancellationToken);

        return new ChangeNumberEligibilityResult(
            true,
            "نقل الرقم (MNP Port-In) مسموح — بانتظار اعتماد الباك أوفيس.",
            profile.CustomerId,
            currentAsset.Msisdn,
            canonicalPortIn,
            currentId,
            null,
            true,
            "PortInAllowed");
    }

    public async Task<ChangeNumberEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operation.NumberChangeReason))
        {
            throw new BusinessRuleViolationException("سبب تغيير الرقم غير مسجل في الطلب.");
        }

        if (ChangeNumberWellKnown.IsPortInMode(operation.NumberChangeMode))
        {
            var currentId = operation.MsisdnAssetId
                ?? throw new BusinessRuleViolationException("الرقم الحالي غير محدد في الطلب.");
            var result = await ValidateForPortInCreateAsync(
                operation.SubscriberProfileId,
                currentId,
                operation.PortInMsisdn ?? string.Empty,
                operation.DonorOperatorCode ?? string.Empty,
                operation.NumberChangeReason,
                operation.AgencyReference,
                operation.Id,
                cancellationToken);
            return result.Allowed
                ? result
                : result;
        }

        var currentIdInternal = operation.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("الرقم الحالي غير محدد في الطلب.");
        var targetId = operation.TargetMsisdnAssetId
            ?? throw new BusinessRuleViolationException("الرقم المستهدف غير محدد في الطلب.");

        var resultInternal = await ValidateForCreateAsync(
            operation.SubscriberProfileId,
            currentIdInternal,
            targetId,
            operation.NumberChangeReason,
            operation.PremiumFeeAmount,
            operation.Id,
            cancellationToken);

        if (!resultInternal.Allowed)
        {
            return resultInternal;
        }

        if (resultInternal.RequiresBackOfficeApproval
            && !ChangeNumberWellKnown.IsPremiumFeeSatisfied(
                await LoadTargetCategoryAsync(targetId, cancellationToken),
                operation.PremiumFeeAmount,
                operation.PaymentReference))
        {
            return Deny(
                "VAL-05-02: الرقم المميز يتطلب اعتماد مالي أو إيصال دفع قبل التفعيل.",
                resultInternal.CustomerId,
                resultInternal.CurrentMsisdn,
                resultInternal.TargetMsisdn,
                resultInternal.PriorMsisdnAssetId,
                resultInternal.TargetMsisdnAssetId,
                true,
                "PremiumNotCleared");
        }

        return resultInternal;
    }

    private async Task<MsisdnCategory> LoadTargetCategoryAsync(string targetId, CancellationToken cancellationToken)
    {
        var cat = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Id == targetId)
            .Select(m => m.Category)
            .FirstOrDefaultAsync(cancellationToken);
        return cat;
    }

    private static string? ValidateTargetPoolState(MsisdnAsset target, string customerId)
    {
        if (target.PoolStatus == MsisdnPoolStatus.Quarantined)
        {
            return "VAL-05-03: الرقم المستهدف في حجر صحي ولا يمكن تعيينه.";
        }

        if (target.PoolStatus == MsisdnPoolStatus.Active
            && !string.IsNullOrEmpty(target.SubscriberProfileId))
        {
            return "VAL-NUM-001: الرقم المستهدف مربوط بمشترك آخر.";
        }

        if (target.PoolStatus == MsisdnPoolStatus.Reserved)
        {
            if (target.ReservedUntilUtc.HasValue && target.ReservedUntilUtc < DateTime.UtcNow)
            {
                return "VAL-05-01: انتهت صلاحية حجز الرقم — يرجى إعادة الحجز.";
            }

            if (!string.IsNullOrEmpty(target.ReservedForCustomerId)
                && !string.Equals(target.ReservedForCustomerId, customerId, StringComparison.Ordinal))
            {
                return "VAL-05-01: الرقم محجوز لعميل آخر.";
            }
        }

        if (target.PoolStatus is not (MsisdnPoolStatus.Available or MsisdnPoolStatus.Reserved))
        {
            return $"VAL-NUM-001: الرقم المستهدف غير متاح (الحالة: {target.PoolStatus}).";
        }

        return null;
    }

    private static bool RequiresBackOfficeApproval(
        MsisdnAsset target,
        decimal? premiumFeeAmount,
        string? paymentReference) =>
        ChangeNumberWellKnown.IsPremiumCategory(target.Category)
        && !ChangeNumberWellKnown.IsPremiumFeeSatisfied(target.Category, premiumFeeAmount, paymentReference);

    private static void EnsureCustomerEligible(Customer? customer)
    {
        if (customer == null)
        {
            throw new BusinessRuleViolationException("العميل غير موجود.");
        }

        if (customer.Status == CustomerStatus.Blacklisted)
        {
            throw new BusinessRuleViolationException("VAL-05-04: العميل على القائمة السوداء — لا يمكن تغيير الرقم.");
        }

        if (customer.Status == CustomerStatus.Closed)
        {
            throw new BusinessRuleViolationException("VAL-05-04: حساب العميل مغلق.");
        }
    }

    private async Task EnsureNoOutstandingDebtAsync(string msisdn, CancellationToken cancellationToken)
    {
        var balance = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
        if (balance < 0)
        {
            throw new BusinessRuleViolationException(
                $"VAL-05-04: لا يمكن تغيير الرقم — ذمم مالية معلقة بقيمة {-balance:N0} ل.س على الخط {msisdn}.");
        }
    }

    private async Task EnsureNoBlockingChangeNumberAsync(
        string msisdnAssetId,
        string? excludeOperationId,
        CancellationToken cancellationToken)
    {
        var query = _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.Kind == TelecomOperationKind.NumberPortability
                        && (o.MsisdnAssetId == msisdnAssetId || o.TargetMsisdnAssetId == msisdnAssetId)
                        && BlockingStatuses.Contains(o.Status));

        if (!string.IsNullOrEmpty(excludeOperationId))
        {
            query = query.Where(o => o.Id != excludeOperationId);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessRuleViolationException(
                "VAL-05-05: يوجد طلب تغيير رقم مفتوح على نفس الخط أو الرقم المستهدف.");
        }
    }

    private static ChangeNumberEligibilityResult Deny(
        string messageAr,
        string? customerId,
        string? currentMsisdn,
        string? targetMsisdn,
        string? priorId,
        string? targetId,
        bool requiresBo,
        string code) =>
        new(false, messageAr, customerId, currentMsisdn, targetMsisdn, priorId, targetId, requiresBo, code);
}
