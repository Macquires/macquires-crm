using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OfferSubscription;

public sealed class OfferSubscriptionEligibilityChecker : IOfferSubscriptionEligibilityChecker
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

    public OfferSubscriptionEligibilityChecker(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public Task<OfferSubscriptionEligibilityResult> ValidateForMigrationCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string productOfferingId,
        string resolvedProductId,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default) =>
        ValidateMigrationCoreAsync(
            subscriberProfileId,
            msisdnAssetId,
            productOfferingId,
            resolvedProductId,
            excludeOperationId,
            cancellationToken);

    public Task<OfferSubscriptionEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.Migration)
        {
            return Task.FromResult(Allow("NotMigration"));
        }

        var offeringId = (operation.ProductOfferingId ?? string.Empty).Trim();
        var productId = (operation.ProductId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(offeringId) || string.IsNullOrEmpty(productId))
        {
            return Task.FromResult(Deny(
                OfferSubscriptionWellKnown.Format("01", "العرض التجاري أو المنتج التقني غير محدد."),
                null,
                operation.MsisdnAssetId,
                null,
                null,
                "MissingCatalog"));
        }

        return ValidateMigrationCoreAsync(
            operation.SubscriberProfileId,
            operation.MsisdnAssetId ?? string.Empty,
            offeringId,
            productId,
            operation.Id,
            cancellationToken);
    }

    public async Task<OfferSubscriptionEligibilityResult> ValidateForVasToggleAsync(
        string msisdn,
        string serviceCode,
        bool activate,
        CancellationToken cancellationToken = default)
    {
        var canonical = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(msisdn)
            ?? throw new BusinessRuleViolationException("رقم الخط غير صالح.");

        var subscription = await _query.TelecomSubscription
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(s => s.MsisdnAsset)
            .Include(s => s.SubscriptionTypeLookup)
            .Include(s => s.SubscriberProfile)
            .ThenInclude(p => p!.Customer)
            .FirstOrDefaultAsync(
                s => s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == canonical,
                cancellationToken)
            ?? throw new BusinessRuleViolationException("لا يوجد اشتراك لهذا الرقم.");

        if (subscription.SubscriberProfile?.OperationalStatus == SubscriberOperationalStatus.Terminated)
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("02", "لا يمكن تعديل الخدمات — ملف المشترك منتهٍ."),
                canonical,
                subscription.MsisdnAssetId,
                null,
                null,
                "ProfileTerminated");
        }

        var asset = subscription.MsisdnAsset;
        if (asset == null || asset.PoolStatus != MsisdnPoolStatus.Active)
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("03", "الخط غير نشط — لا يمكن تفعيل أو إلغاء VAS."),
                canonical,
                subscription.MsisdnAssetId,
                null,
                null,
                "LineNotActive");
        }

        var code = serviceCode.Trim().ToUpperInvariant();
        var vas = await _query.TelecomValueAddedService
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .FirstOrDefaultAsync(x => x.IsActive && x.ServiceCode == code, cancellationToken)
            ?? throw new BusinessRuleViolationException($"خدمة VAS '{code}' غير موجودة أو غير فعّالة.");

        if (!activate)
        {
            return Allow(canonical, subscription.MsisdnAssetId, subscription.ProductId, subscription.ProductOfferingId, "VasDeactivate");
        }

        var offering = await FindVasOfferingByServiceCodeAsync(code, cancellationToken);
        if (offering != null)
        {
            var windowCheck = ValidateOfferingWindow(offering);
            if (windowCheck != null)
            {
                return Deny(windowCheck, canonical, subscription.MsisdnAssetId, null, null, "OfferingExpired");
            }

            var compat = ValidateOfferingCompatibleWithLine(offering, subscription);
            if (compat != null)
            {
                return Deny(compat, canonical, subscription.MsisdnAssetId, null, null, "IncompatibleLine");
            }

            var prereq = await ValidateComponentPrerequisitesAsync(offering.Id, subscription, cancellationToken);
            if (prereq != null)
            {
                return Deny(prereq, canonical, subscription.MsisdnAssetId, null, null, "MissingPrerequisite");
            }
        }

        if (!string.IsNullOrEmpty(canonical))
        {
            await EnsureNoOutstandingDebtAsync(canonical, cancellationToken);
        }

        var blocking = await HasBlockingOperationsAsync(
            subscription.SubscriberProfileId,
            subscription.MsisdnAssetId,
            null,
            cancellationToken);
        if (blocking)
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("04", "يوجد طلب تشغيلي معلّق على هذا الخط."),
                canonical,
                subscription.MsisdnAssetId,
                null,
                null,
                "BlockingOperation");
        }

        return Allow(canonical, subscription.MsisdnAssetId, subscription.ProductId, subscription.ProductOfferingId, "VasActivate");
    }

    private async Task<OfferSubscriptionEligibilityResult> ValidateMigrationCoreAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string productOfferingId,
        string resolvedProductId,
        string? excludeOperationId,
        CancellationToken cancellationToken)
    {
        var profileId = subscriberProfileId.Trim();
        var assetId = msisdnAssetId.Trim();
        var offeringId = productOfferingId.Trim();
        var productId = resolvedProductId.Trim();

        if (string.IsNullOrEmpty(assetId))
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("05", "يجب تحديد خط المشترك لترحيل الباقة."),
                null,
                null,
                null,
                null,
                "MissingMsisdnAsset");
        }

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        if (profile.OperationalStatus == SubscriberOperationalStatus.Terminated)
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("02", "لا يمكن ترحيل الباقة — ملف المشترك منتهٍ."),
                null,
                assetId,
                null,
                null,
                "ProfileTerminated");
        }

        var asset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        if (asset.PoolStatus != MsisdnPoolStatus.Active)
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("03", $"الخط غير نشط ({asset.PoolStatus})."),
                asset.Msisdn,
                assetId,
                null,
                null,
                "LineNotActive");
        }

        if (!string.Equals(asset.SubscriberProfileId, profileId, StringComparison.Ordinal))
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("06", "الخط غير مربوط بملف المشترك."),
                asset.Msisdn,
                assetId,
                null,
                null,
                "LineNotOwned");
        }

        var subscription = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo(false)
            .Include(s => s.SubscriptionTypeLookup)
            .FirstOrDefaultAsync(
                s => s.SubscriberProfileId == profileId && s.MsisdnAssetId == assetId,
                cancellationToken)
            ?? throw new BusinessRuleViolationException("لا يوجد اشتراك على هذا الخط.");

        var offering = await _query.ProductOffering.AsNoTracking().IsDeletedEqualTo(false)
            .FirstOrDefaultAsync(o => o.Id == offeringId, cancellationToken)
            ?? throw new BusinessRuleViolationException("العرض التجاري المختار غير موجود.");

        if (!offering.IsActive)
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("07", "العرض التجاري غير فعّال."),
                asset.Msisdn,
                assetId,
                subscription.ProductId,
                subscription.ProductOfferingId,
                "OfferingInactive");
        }

        var windowMsg = ValidateOfferingWindow(offering);
        if (windowMsg != null)
        {
            return Deny(windowMsg, asset.Msisdn, assetId, subscription.ProductId, subscription.ProductOfferingId, "OfferingExpired");
        }

        var compatMsg = ValidateOfferingCompatibleWithLine(offering, subscription);
        if (compatMsg != null)
        {
            return Deny(compatMsg, asset.Msisdn, assetId, subscription.ProductId, subscription.ProductOfferingId, "IncompatibleLine");
        }

        var product = await _query.Product.AsNoTracking().IsDeletedEqualTo(false)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new BusinessRuleViolationException("المنتج التقني المرتبط غير موجود.");

        if (!string.IsNullOrEmpty(product.CompatibleSubscriptionTypeId)
            && product.CompatibleSubscriptionTypeId != subscription.SubscriptionTypeId)
        {
            return Deny(
                OfferSubscriptionWellKnown.Format(
                    "08",
                    $"نوع الخط ({subscription.SubscriptionTypeLookup?.NameAr ?? "—"}) غير متوافق مع الباقة المطلوبة."),
                asset.Msisdn,
                assetId,
                subscription.ProductId,
                subscription.ProductOfferingId,
                "ProductIncompatible");
        }

        if (string.Equals(subscription.ProductId, productId, StringComparison.Ordinal)
            && string.Equals(subscription.ProductOfferingId, offeringId, StringComparison.Ordinal))
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("09", "المشترك مشترك بالفعل بهذا العرض."),
                asset.Msisdn,
                assetId,
                subscription.ProductId,
                subscription.ProductOfferingId,
                "AlreadyOnOffer");
        }

        var prereqMsg = await ValidateComponentPrerequisitesAsync(offeringId, subscription, cancellationToken);
        if (prereqMsg != null)
        {
            return Deny(prereqMsg, asset.Msisdn, assetId, subscription.ProductId, subscription.ProductOfferingId, "MissingPrerequisite");
        }

        if (!string.IsNullOrEmpty(asset.Msisdn))
        {
            await EnsureNoOutstandingDebtAsync(asset.Msisdn, cancellationToken);
        }

        if (await HasBlockingOperationsAsync(profileId, assetId, excludeOperationId, cancellationToken))
        {
            return Deny(
                OfferSubscriptionWellKnown.Format("04", "يوجد طلب تشغيلي معلّق على هذا الخط."),
                asset.Msisdn,
                assetId,
                subscription.ProductId,
                subscription.ProductOfferingId,
                "BlockingOperation");
        }

        return Allow(
            asset.Msisdn,
            assetId,
            subscription.ProductId,
            subscription.ProductOfferingId,
            "MigrationAllowed");
    }

    private static string? ValidateOfferingWindow(ProductOffering offering)
    {
        var now = DateTime.UtcNow;
        if (offering.ValidFromUtc.HasValue && offering.ValidFromUtc > now)
        {
            return OfferSubscriptionWellKnown.Format("10", "العرض التجاري لم يبدأ بعد.");
        }

        if (offering.ValidToUtc.HasValue && offering.ValidToUtc < now)
        {
            return OfferSubscriptionWellKnown.Format("10", "العرض التجاري منتهي الصلاحية.");
        }

        return null;
    }

    private static string? ValidateOfferingCompatibleWithLine(ProductOffering offering, TelecomSubscription subscription)
    {
        if (!string.IsNullOrEmpty(offering.CompatibleSubscriptionTypeId)
            && offering.CompatibleSubscriptionTypeId != subscription.SubscriptionTypeId)
        {
            return OfferSubscriptionWellKnown.Format(
                "08",
                "العرض غير متوافق مع نوع الخط الحالي.");
        }

        return null;
    }

    private async Task<string?> ValidateComponentPrerequisitesAsync(
        string offeringId,
        TelecomSubscription subscription,
        CancellationToken cancellationToken)
    {
        var requiredIds = await _query.ProductOfferingComponent.AsNoTracking().IsDeletedEqualTo(false)
            .Where(c => c.ProductOfferingId == offeringId && c.RequiresProductOfferingId != null)
            .Select(c => c.RequiresProductOfferingId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (requiredIds.Count == 0)
        {
            return null;
        }

        var profileOfferings = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo(false)
            .Where(s => s.SubscriberProfileId == subscription.SubscriberProfileId && s.ProductOfferingId != null)
            .Select(s => s.ProductOfferingId!)
            .ToListAsync(cancellationToken);

        foreach (var req in requiredIds)
        {
            if (profileOfferings.Any(id => string.Equals(id, req, StringComparison.Ordinal))
                || string.Equals(subscription.ProductOfferingId, req, StringComparison.Ordinal))
            {
                continue;
            }

            var name = await _query.ProductOffering.AsNoTracking()
                .Where(o => o.Id == req)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);

            return OfferSubscriptionWellKnown.Format(
                "11",
                $"يتطلب العرض تفعيل الباقة الأساسية أولاً ({name ?? req}).");
        }

        return null;
    }

    private async Task<ProductOffering?> FindVasOfferingByServiceCodeAsync(
        string serviceCode,
        CancellationToken cancellationToken)
    {
        var socMatch = await _query.ProductOffering.AsNoTracking().IsDeletedEqualTo(false)
            .Where(o => o.IsActive && (o.Code == serviceCode || o.ServiceIdSocCode == serviceCode))
            .FirstOrDefaultAsync(cancellationToken);
        if (socMatch != null)
        {
            return socMatch;
        }

        return await (
            from c in _query.ProductOfferingComponent.AsNoTracking().IsDeletedEqualTo(false)
            where c.ComponentType == ServiceComponentType.Vas
            join o in _query.ProductOffering.AsNoTracking().IsDeletedEqualTo(false)
                on c.ProductOfferingId equals o.Id
            where o.IsActive && (o.Code == serviceCode || o.ServiceIdSocCode == serviceCode)
            select o
        ).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureNoOutstandingDebtAsync(string msisdn, CancellationToken cancellationToken)
    {
        var balance = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
        if (balance < 0)
        {
            throw new BusinessRuleViolationException(
                OfferSubscriptionWellKnown.Format("12", "لا يمكن تنفيذ العملية — يوجد رصيد مستحق على الخط."));
        }
    }

    private async Task<bool> HasBlockingOperationsAsync(
        string profileId,
        string? msisdnAssetId,
        string? excludeOperationId,
        CancellationToken cancellationToken)
    {
        var q = _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo(false)
            .Where(o => o.SubscriberProfileId == profileId && BlockingStatuses.Contains(o.Status));

        if (!string.IsNullOrEmpty(msisdnAssetId))
        {
            q = q.Where(o => o.MsisdnAssetId == null || o.MsisdnAssetId == msisdnAssetId);
        }

        if (!string.IsNullOrEmpty(excludeOperationId))
        {
            q = q.Where(o => o.Id != excludeOperationId);
        }

        return await q.AnyAsync(cancellationToken);
    }

    private static OfferSubscriptionEligibilityResult Allow(
        string? msisdn = null,
        string? msisdnAssetId = null,
        string? priorProductId = null,
        string? priorProductOfferingId = null,
        string code = "Allowed") =>
        new(true, "مسموح", msisdn, msisdnAssetId, priorProductId, priorProductOfferingId, code);

    private static OfferSubscriptionEligibilityResult Deny(
        string messageAr,
        string? msisdn,
        string? msisdnAssetId,
        string? priorProductId,
        string? priorProductOfferingId,
        string code) =>
        new(false, messageAr, msisdn, msisdnAssetId, priorProductId, priorProductOfferingId, code);
}
