using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Application.Common.CQS.Queries;

namespace Application.Common.Telecom;

public sealed record TelecomLineProvisionContext(
    string? Msisdn,
    string? Iccid,
    string? Imsi,
    string? ProductServiceCode,
    string? SubscriptionTypeCode,
    decimal? InitialDeposit,
    string? SourceSubscriptionTypeCode = null,
    string? TargetSubscriptionTypeCode = null,
    string? PriorIccid = null,
    string? PriorMsisdn = null);

/// <summary>Builds CBS/HLR requests from operation + pool/inventory context.</summary>
public static class TelecomProvisionRequestBuilder
{
    public static async Task<TelecomLineProvisionContext> ResolveLineContextAsync(
        IQueryContext query,
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        string? msisdn = null;
        string? imsi = null;
        string? pairedIccid = null;

        string? priorMsisdn = null;

        if (operation.Kind == TelecomOperationKind.NumberPortability)
        {
            if (!string.IsNullOrEmpty(operation.TargetMsisdnAssetId))
            {
                var target = await query.MsisdnAsset.AsNoTracking()
                    .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == operation.TargetMsisdnAssetId, cancellationToken);
                msisdn = target?.Msisdn;
                imsi = target?.PairedImsi;
                pairedIccid = target?.PairedIccid;
            }

            var priorId = operation.PriorMsisdnAssetId ?? operation.MsisdnAssetId;
            if (!string.IsNullOrEmpty(priorId))
            {
                priorMsisdn = await query.MsisdnAsset.AsNoTracking()
                    .Where(m => !m.IsDeleted && m.Id == priorId)
                    .Select(m => m.Msisdn)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }
        else if (!string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            var asset = await query.MsisdnAsset.AsNoTracking()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId, cancellationToken);
            msisdn = asset?.Msisdn;
            imsi = asset?.PairedImsi;
            pairedIccid = asset?.PairedIccid;
        }

        string? simIccid = null;
        if (!string.IsNullOrEmpty(operation.SimInventoryId))
        {
            var sim = await query.SimInventory.AsNoTracking()
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == operation.SimInventoryId, cancellationToken);
            simIccid = sim?.Iccid;
            imsi ??= sim?.Imsi;
        }

        var iccid = simIccid ?? pairedIccid;

        string? priorIccid = null;
        if (!string.IsNullOrEmpty(operation.PriorSimInventoryId))
        {
            priorIccid = await query.SimInventory.AsNoTracking()
                .Where(s => !s.IsDeleted && s.Id == operation.PriorSimInventoryId)
                .Select(s => s.Iccid)
                .FirstOrDefaultAsync(cancellationToken);
        }

        string? serviceCode = null;
        decimal? initialDeposit = null;
        string? subscriptionTypeCode = null;

        if (!string.IsNullOrEmpty(operation.ProductId))
        {
            var product = await query.Product.AsNoTracking()
                .FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == operation.ProductId, cancellationToken);
            serviceCode = product?.ServiceCode;
            if (product?.UnitPrice is > 0 && operation.Kind == TelecomOperationKind.NewActivation)
            {
                initialDeposit = (decimal)product.UnitPrice;
            }
        }

        string? sourceSubscriptionTypeCode = null;
        string? targetSubscriptionTypeCode = null;

        if (operation.Kind == TelecomOperationKind.ChangeGsmType)
        {
            if (!string.IsNullOrEmpty(operation.SourceSubscriptionTypeId))
            {
                sourceSubscriptionTypeCode = await query.TelecomSubscriptionTypeLookup.AsNoTracking()
                    .Where(t => !t.IsDeleted && t.Id == operation.SourceSubscriptionTypeId)
                    .Select(t => t.Code)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!string.IsNullOrEmpty(operation.TargetSubscriptionTypeId))
            {
                targetSubscriptionTypeCode = await query.TelecomSubscriptionTypeLookup.AsNoTracking()
                    .Where(t => !t.IsDeleted && t.Id == operation.TargetSubscriptionTypeId)
                    .Select(t => t.Code)
                    .FirstOrDefaultAsync(cancellationToken);
                subscriptionTypeCode = targetSubscriptionTypeCode;
            }
        }
        else if (operation.Kind == TelecomOperationKind.NewActivation
                 && !string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            subscriptionTypeCode = await (
                from m in query.MsisdnAsset.AsNoTracking()
                where !m.IsDeleted && m.Id == operation.MsisdnAssetId
                join t in query.TelecomSubscriptionTypeLookup.AsNoTracking()
                    on (m.IntendedSubscriptionTypeId ?? m.Product!.CompatibleSubscriptionTypeId) equals t.Id
                select t.Code
            ).FirstOrDefaultAsync(cancellationToken);

            subscriptionTypeCode ??= await ResolveSubscriptionTypeCodeAsync(
                query,
                operation.SubscriberProfileId,
                operation.MsisdnAssetId,
                cancellationToken);
        }
        else if (!string.IsNullOrEmpty(operation.SubscriberProfileId))
        {
            subscriptionTypeCode = await ResolveSubscriptionTypeCodeAsync(
                query,
                operation.SubscriberProfileId,
                operation.MsisdnAssetId,
                cancellationToken);
        }

        MsisdnAsset? kitAsset = null;
        if (!string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            kitAsset = await query.MsisdnAsset.AsNoTracking()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId, cancellationToken);
        }
        else if (operation.Kind == TelecomOperationKind.NumberPortability
                 && !string.IsNullOrEmpty(operation.TargetMsisdnAssetId))
        {
            kitAsset = await query.MsisdnAsset.AsNoTracking()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == operation.TargetMsisdnAssetId, cancellationToken);
        }

        if (kitAsset != null)
        {
            var (resolvedIccid, resolvedImsi) = await MsisdnAssetKitResolver.ResolveForAssetAsync(
                query,
                kitAsset,
                operation.SubscriberProfileId,
                operation.SimInventoryId,
                cancellationToken);
            iccid = resolvedIccid ?? iccid;
            imsi = resolvedImsi ?? imsi;
            msisdn ??= kitAsset.Msisdn;
        }

        return new TelecomLineProvisionContext(
            msisdn,
            iccid,
            imsi,
            serviceCode,
            subscriptionTypeCode,
            initialDeposit,
            sourceSubscriptionTypeCode,
            targetSubscriptionTypeCode,
            priorIccid,
            priorMsisdn);
    }

    public static BillingProvisionRequest ToBillingRequest(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext line,
        TelecomBillingProvisionPhase phase = TelecomBillingProvisionPhase.Provision) =>
        new(
            operation.Id,
            operation.Number,
            line.Msisdn,
            operation.Kind,
            operation.CorrelationId,
            line.InitialDeposit,
            line.ProductServiceCode,
            line.SubscriptionTypeCode,
            line.Imsi,
            line.Iccid,
            phase,
            line.PriorIccid,
            line.PriorMsisdn);

    public static NetworkProvisionRequest ToNetworkRequest(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext line) =>
        new(
            operation.Id,
            operation.Number,
            line.Msisdn,
            line.Iccid,
            operation.CorrelationId,
            operation.Kind,
            line.Imsi,
            line.ProductServiceCode,
            line.SubscriptionTypeCode,
            line.PriorMsisdn);

    private static async Task<string?> ResolveSubscriptionTypeCodeAsync(
        IQueryContext query,
        string subscriberProfileId,
        string? msisdnAssetId,
        CancellationToken cancellationToken)
    {
        var subs = query.TelecomSubscription.AsNoTracking()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == subscriberProfileId);

        if (!string.IsNullOrEmpty(msisdnAssetId))
        {
            subs = subs.Where(s => s.MsisdnAssetId == msisdnAssetId);
        }

        return await (
            from s in subs
            join t in query.TelecomSubscriptionTypeLookup.AsNoTracking() on s.SubscriptionTypeId equals t.Id
            orderby s.IsPrimaryLine descending
            select t.Code
        ).FirstOrDefaultAsync(cancellationToken);
    }
}
