using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.SellingLine;

public sealed class SellingLineEligibilityChecker : ISellingLineEligibilityChecker
{
    private const string DealerChannelMissingCode = "VAL-ACT-09: Dealer Code is missing for Dealer Channel.";
    private const string DealerChannelUnknownCode = "VAL-ACT-09: Dealer code is not registered in the system.";

    private readonly IQueryContext _query;
    private readonly IDealerCodeValidator _dealerCodeValidator;

    public SellingLineEligibilityChecker(IQueryContext query, IDealerCodeValidator dealerCodeValidator)
    {
        _query = query;
        _dealerCodeValidator = dealerCodeValidator;
    }

    public async Task ValidateForCreateAsync(
        CreateTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Kind != TelecomOperationKind.NewActivation)
        {
            return;
        }

        if ((request.ActivationChannel ?? ActivationChannel.Showroom) == ActivationChannel.Dealer)
        {
            var dealerCode = (request.DealerCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(dealerCode))
            {
                throw new BusinessRuleViolationException(DealerChannelMissingCode);
            }

            if (!await _dealerCodeValidator.ExistsAsync(dealerCode, cancellationToken))
            {
                throw new BusinessRuleViolationException(DealerChannelUnknownCode);
            }
        }

        await ValidateMsisdnAsync(request.MsisdnAssetId, cancellationToken);
        await ValidateActivationLineTypeMatchAsync(
            request.MsisdnAssetId,
            request.TargetSubscriptionTypeId,
            cancellationToken);
        await ValidateSimAsync(request.SimInventoryId, request.SimIccid, cancellationToken);
    }

    public async Task ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (operation.Kind != TelecomOperationKind.NewActivation)
        {
            return;
        }

        ValidateKycGate(operation);
        await ValidateMsisdnAsync(operation.MsisdnAssetId, cancellationToken);
        await ValidateActivationLineTypeMatchAsync(
            operation.MsisdnAssetId,
            operation.TargetSubscriptionTypeId,
            cancellationToken);
        await ValidateSimForOperationAsync(operation.SimInventoryId, cancellationToken);

        if (!string.IsNullOrEmpty(operation.ProductId))
        {
            await ValidateCatalogForOperationAsync(operation, cancellationToken);
        }
    }

    public Task ValidateCatalogForCreateAsync(
        CreateTelecomOperationRequest request,
        string resolvedProductId,
        string? resolvedOfferingId,
        CancellationToken cancellationToken)
    {
        if (request.Kind != TelecomOperationKind.NewActivation)
        {
            return Task.CompletedTask;
        }

        var stub = new TelecomOperationRequest
        {
            SubscriberProfileId = request.SubscriberProfileId,
            MsisdnAssetId = request.MsisdnAssetId,
            ProductId = resolvedProductId,
            ProductOfferingId = resolvedOfferingId,
            TargetSubscriptionTypeId = request.TargetSubscriptionTypeId,
        };

        return ValidateCatalogForOperationAsync(stub, cancellationToken);
    }

    private static void ValidateKycGate(TelecomOperationRequest operation)
    {
        if (!string.IsNullOrWhiteSpace(operation.OverrideReasonCode))
        {
            return;
        }

        if (operation.KycVerifiedAtUtc.HasValue)
        {
            return;
        }

        if (operation.DocumentStatus is TelecomDocumentStatus.Verified or TelecomDocumentStatus.Uploaded)
        {
            return;
        }

        throw new BusinessRuleViolationException(
            "VAL-02-01: يجب اعتماد KYC (رفع الوثيقة أو KycVerifiedAtUtc) قبل تأكيد التفعيل، أو تسجيل سبب استثناء معتمد.");
    }

    private async Task ValidateMsisdnAsync(string? msisdnAssetId, CancellationToken cancellationToken)
    {
        var id = (msisdnAssetId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
        {
            throw new BusinessRuleViolationException("VAL-02-02: رقم MSISDN مطلوب.");
        }

        var asset = await _query.MsisdnAsset.AsNoTracking()
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == id, cancellationToken)
            ?? throw new BusinessRuleViolationException("VAL-02-02: أصل الرقم غير موجود.");

        if (asset.PoolStatus is not (MsisdnPoolStatus.Available or MsisdnPoolStatus.Reserved))
        {
            throw new BusinessRuleViolationException(
                $"VAL-02-02: حالة الرقم {asset.Msisdn} يجب أن تكون متاحاً أو محجوزاً (الحالية: {asset.PoolStatus}).");
        }
    }

    private async Task ValidateSimAsync(
        string? simInventoryId,
        string? simIccid,
        CancellationToken cancellationToken)
    {
        SimInventory? sim = null;
        if (!string.IsNullOrWhiteSpace(simInventoryId))
        {
            sim = await _query.SimInventory.AsNoTracking()
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == simInventoryId.Trim(), cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(simIccid))
        {
            if (!IccidValidator.TryValidate(simIccid, out var normalized, out var err))
            {
                throw new BusinessRuleViolationException($"VAL-02-03: {err}");
            }

            sim = await _query.SimInventory.AsNoTracking()
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Iccid == normalized, cancellationToken);
        }

        if (sim == null)
        {
            throw new BusinessRuleViolationException("VAL-02-03: الشريحة (ICCID) غير موجودة في المستودع.");
        }

        if (sim.Status is not (SimStatus.Available or SimStatus.Reserved))
        {
            throw new BusinessRuleViolationException(
                $"VAL-02-03: الشريحة غير متاحة للتفعيل (الحالة: {sim.Status}).");
        }
    }

    private async Task ValidateSimForOperationAsync(string? simInventoryId, CancellationToken cancellationToken)
    {
        var id = (simInventoryId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
        {
            throw new BusinessRuleViolationException("VAL-02-03: الشريحة مطلوبة قبل التأكيد.");
        }

        await ValidateSimAsync(id, null, cancellationToken);
    }

    private async Task ValidateActivationLineTypeMatchAsync(
        string? msisdnAssetId,
        string? selectedLineTypeId,
        CancellationToken cancellationToken)
    {
        var msisdnId = (msisdnAssetId ?? string.Empty).Trim();
        var lineTypeId = (selectedLineTypeId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(msisdnId) || string.IsNullOrEmpty(lineTypeId))
        {
            return;
        }

        var asset = await _query.MsisdnAsset.AsNoTracking()
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == msisdnId, cancellationToken);

        var intended = asset?.IntendedSubscriptionTypeId;
        if (!string.IsNullOrEmpty(intended)
            && !string.Equals(intended, lineTypeId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleViolationException(
                "VAL-02-06: نوع الخط المختار لا يطابق نوع الرقم المثبّت في مستودع MSISDN.");
        }
    }

    private async Task ValidateCatalogForOperationAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        var lineTypeId = await ResolveCatalogLineTypeIdAsync(operation, cancellationToken);
        if (string.IsNullOrEmpty(lineTypeId))
        {
            throw new BusinessRuleViolationException(
                "VAL-02-04: لا يمكن تحديد نوع الخط للتحقق من توافق العرض.");
        }

        var lineTypeName = await _query.TelecomSubscriptionTypeLookup.AsNoTracking()
            .Where(t => !t.IsDeleted && t.Id == lineTypeId)
            .Select(t => t.NameAr)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        var productId = operation.ProductId!;

        var product = await _query.Product.AsNoTracking()
            .FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == productId, cancellationToken)
            ?? throw new BusinessRuleViolationException("VAL-02-04: المنتج التقني غير موجود.");

        if (!string.IsNullOrEmpty(operation.ProductOfferingId))
        {
            var offering = await _query.ProductOffering.AsNoTracking()
                .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == operation.ProductOfferingId, cancellationToken);

            if (offering != null
                && !string.IsNullOrEmpty(offering.CompatibleSubscriptionTypeId)
                && offering.CompatibleSubscriptionTypeId != lineTypeId)
            {
                throw new BusinessRuleViolationException(
                    $"VAL-02-04: نوع الخط ({lineTypeName}) غير متوافق مع العرض التجاري.");
            }
        }

        if (!string.IsNullOrEmpty(product.CompatibleSubscriptionTypeId)
            && product.CompatibleSubscriptionTypeId != lineTypeId)
        {
            throw new BusinessRuleViolationException(
                $"VAL-02-04: نوع الخط ({lineTypeName}) غير متوافق مع الباقة.");
        }
    }

    private async Task<string?> ResolveCatalogLineTypeIdAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(operation.TargetSubscriptionTypeId))
        {
            return operation.TargetSubscriptionTypeId.Trim();
        }

        var msisdnId = (operation.MsisdnAssetId ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(msisdnId))
        {
            var asset = await _query.MsisdnAsset.AsNoTracking()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == msisdnId, cancellationToken);

            var fromAsset = asset == null ? null : MsisdnAssetLineTypeResolver.ResolveTypeId(asset);
            if (!string.IsNullOrEmpty(fromAsset))
            {
                return fromAsset;
            }
        }

        var subs = await _query.TelecomSubscription.AsNoTracking()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == operation.SubscriberProfileId)
            .Include(s => s.SubscriptionTypeLookup)
            .ToListAsync(cancellationToken);

        if (subs.Count == 0)
        {
            return null;
        }

        TelecomSubscription? chosen = null;
        if (!string.IsNullOrEmpty(msisdnId))
        {
            chosen = subs.FirstOrDefault(s => s.MsisdnAssetId == msisdnId);
        }

        chosen ??= subs.FirstOrDefault(s => s.IsPrimaryLine) ?? subs.First();
        return chosen.SubscriptionTypeId;
    }
}
