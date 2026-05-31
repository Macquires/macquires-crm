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
    decimal? InitialDeposit);

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

        if (!string.IsNullOrEmpty(operation.MsisdnAssetId))
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

        if (!string.IsNullOrEmpty(operation.SubscriberProfileId))
        {
            subscriptionTypeCode = await (
                from s in query.TelecomSubscription.AsNoTracking()
                join t in query.TelecomSubscriptionTypeLookup.AsNoTracking() on s.SubscriptionTypeId equals t.Id
                where !s.IsDeleted && s.SubscriberProfileId == operation.SubscriberProfileId
                orderby s.IsPrimaryLine descending
                select t.Code
            ).FirstOrDefaultAsync(cancellationToken);
        }

        return new TelecomLineProvisionContext(
            msisdn,
            iccid,
            imsi,
            serviceCode,
            subscriptionTypeCode,
            initialDeposit);
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
            phase);

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
            line.SubscriptionTypeCode);
}
