using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.SimSwap;

public sealed class SimSwapEligibilityChecker : ISimSwapEligibilityChecker
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

    public SimSwapEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<SimSwapEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string? msisdnAssetId,
        string? simInventoryId,
        string? simIccid,
        string replacementReason,
        bool isLostOrStolenReport,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(replacementReason))
        {
            throw new BusinessRuleViolationException("سبب تبديل الشريحة مطلوب.");
        }

        var profileId = subscriberProfileId.Trim();
        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        EnsureCustomerEligible(profile.Customer);

        var newSim = await ResolveNewSimAsync(simInventoryId, simIccid, cancellationToken);
        var newSimId = newSim.Id;

        if (!IccidValidator.TryValidate(newSim.Iccid, out _, out var iccidError))
        {
            return Deny($"VAL-04-01: {iccidError}", profile.CustomerId, null, null, newSimId, "InvalidIccid");
        }

        if (newSim.Status is not (SimStatus.Available or SimStatus.Reserved))
        {
            return Deny(
                "VAL-04-01: الشريحة الجديدة غير متاحة في المستودع.",
                profile.CustomerId,
                null,
                null,
                newSimId,
                "SimNotAvailable");
        }

        if (!string.IsNullOrEmpty(newSim.SubscriberProfileId)
            && !string.Equals(newSim.SubscriberProfileId, profileId, StringComparison.Ordinal)
            && newSim.Status == SimStatus.Active)
        {
            return Deny(
                "VAL-04-01: الشريحة الجديدة مربوطة بملف مشترك آخر.",
                profile.CustomerId,
                null,
                null,
                newSimId,
                "SimAssignedElsewhere");
        }

        var priorSim = await ResolvePriorActiveSimAsync(profileId, newSimId, cancellationToken);
        if (priorSim == null)
        {
            return Deny(
                "VAL-04-01: لا توجد شريحة نشطة على ملف المشترك للاستبدال.",
                profile.CustomerId,
                null,
                null,
                newSimId,
                "NoActiveSim");
        }

        if (string.Equals(priorSim.Id, newSimId, StringComparison.Ordinal)
            || string.Equals(priorSim.Iccid, newSim.Iccid, StringComparison.Ordinal))
        {
            return Deny(
                "VAL-04-01: الشريحة الجديدة مطابقة للشريحة الحالية.",
                profile.CustomerId,
                null,
                priorSim.Id,
                newSimId,
                "SameSim");
        }

        var msisdnId = (msisdnAssetId ?? string.Empty).Trim();
        MsisdnAsset? asset = null;
        if (!string.IsNullOrEmpty(msisdnId))
        {
            asset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
                .FirstOrDefaultAsync(m => m.Id == msisdnId, cancellationToken)
                ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

            if (asset.PoolStatus != MsisdnPoolStatus.Active)
            {
                return Deny(
                    $"VAL-04-01: لا يمكن تبديل الشريحة — حالة الرقم {asset.Msisdn} هي {asset.PoolStatus}.",
                    profile.CustomerId,
                    asset.Msisdn,
                    priorSim.Id,
                    newSimId,
                    "MsisdnNotActive");
            }

            if (!string.IsNullOrEmpty(asset.Msisdn))
            {
                await EnsureNoOutstandingDebtAsync(asset.Msisdn, cancellationToken);
            }

            await EnsureNoBlockingSimSwapAsync(msisdnId, excludeOperationId, cancellationToken);
        }

        var msg = isLostOrStolenReport
            ? "تبديل الشريحة مسموح — بانتظار رفع الوثائق واعتماد الباك أوفيس."
            : "تبديل الشريحة مسموح.";

        return new SimSwapEligibilityResult(
            true,
            msg,
            profile.CustomerId,
            asset?.Msisdn,
            priorSim.Id,
            newSimId,
            "Allowed");
    }

    public void ValidateDocumentsForConfirm(TelecomOperationRequest operation)
    {
        if (operation.Kind != TelecomOperationKind.SimSwap || !operation.IsLostOrStolenReport)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(operation.IdentityDocumentStorageKey))
        {
            throw new BusinessRuleViolationException("VAL-04-03: وثيقة الإقرار/الهوية مطلوبة قبل اعتماد التبديل.");
        }

        if (operation.DocumentStatus is not TelecomDocumentStatus.Uploaded and not TelecomDocumentStatus.Verified)
        {
            throw new BusinessRuleViolationException("VAL-04-03: حالة الوثيقة لا تسمح بالاعتماد.");
        }
    }

    public async Task<SimSwapEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        ValidateDocumentsForConfirm(operation);

        if (string.IsNullOrWhiteSpace(operation.ReplacementReason))
        {
            throw new BusinessRuleViolationException("سبب تبديل الشريحة غير مسجل في الطلب.");
        }

        return await ValidateForCreateAsync(
            operation.SubscriberProfileId,
            operation.MsisdnAssetId,
            operation.SimInventoryId,
            null,
            operation.ReplacementReason,
            operation.IsLostOrStolenReport,
            operation.Id,
            cancellationToken);
    }

    private static void EnsureCustomerEligible(Customer? customer)
    {
        if (customer == null)
        {
            throw new BusinessRuleViolationException("العميل غير موجود.");
        }

        if (customer.Status == CustomerStatus.Blacklisted)
        {
            throw new BusinessRuleViolationException("VAL-04-02: العميل على القائمة السوداء — لا يمكن تبديل الشريحة.");
        }

        if (customer.Status == CustomerStatus.Closed)
        {
            throw new BusinessRuleViolationException("VAL-04-02: حساب العميل مغلق.");
        }
    }

    private async Task EnsureNoOutstandingDebtAsync(string msisdn, CancellationToken cancellationToken)
    {
        var balance = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
        if (balance < 0)
        {
            throw new BusinessRuleViolationException(
                $"VAL-04-02: لا يمكن تبديل الشريحة — ذمم مالية معلقة بقيمة {-balance:N0} ل.س على الخط {msisdn}.");
        }
    }

    private async Task EnsureNoBlockingSimSwapAsync(
        string msisdnAssetId,
        string? excludeOperationId,
        CancellationToken cancellationToken)
    {
        var query = _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.Kind == TelecomOperationKind.SimSwap
                        && o.MsisdnAssetId == msisdnAssetId
                        && BlockingStatuses.Contains(o.Status));

        if (!string.IsNullOrEmpty(excludeOperationId))
        {
            query = query.Where(o => o.Id != excludeOperationId);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new BusinessRuleViolationException(
                "VAL-04-01: يوجد طلب تبديل شريحة مفتوح على نفس الخط.");
        }
    }

    private async Task<SimInventory> ResolveNewSimAsync(
        string? simInventoryId,
        string? simIccid,
        CancellationToken cancellationToken)
    {
        var id = (simInventoryId ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(id))
        {
            return await _query.SimInventory.AsNoTracking().IsDeletedEqualTo()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
                ?? throw new BusinessRuleViolationException("الشريحة الجديدة غير موجودة في المستودع.");
        }

        if (!IccidValidator.TryValidate(simIccid, out var normalized, out var err))
        {
            throw new BusinessRuleViolationException(err ?? "ICCID غير صالح.");
        }

        return await _query.SimInventory.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(s => s.Iccid == normalized, cancellationToken)
            ?? throw new BusinessRuleViolationException("الشريحة (ICCID) غير موجودة في المستودع.");
    }

    private async Task<SimInventory?> ResolvePriorActiveSimAsync(
        string profileId,
        string excludeSimId,
        CancellationToken cancellationToken)
    {
        return await _query.SimInventory.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfileId == profileId
                        && s.Status == SimStatus.Active
                        && s.Id != excludeSimId)
            .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static SimSwapEligibilityResult Deny(
        string messageAr,
        string? customerId,
        string? msisdn,
        string? priorSimId,
        string? newSimId,
        string code) =>
        new(false, messageAr, customerId, msisdn, priorSimId, newSimId, code);
}
