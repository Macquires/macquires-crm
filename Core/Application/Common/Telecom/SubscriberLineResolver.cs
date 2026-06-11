using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom;

/// <summary>Resolves the active subscription line and ICCID/IMSI kit for a subscriber profile.</summary>
public sealed record SubscriberLineKitContext(
    string SubscriberProfileId,
    string MsisdnAssetId,
    string Msisdn,
    string Iccid,
    string Imsi,
    MsisdnAsset Asset);

public static class SubscriberLineResolver
{
    public static async Task<SubscriberLineKitContext> ResolveKitAsync(
        IQueryContext query,
        string subscriberProfileId,
        string? msisdnAssetId = null,
        string? msisdn = null,
        string? operationSimInventoryId = null,
        CancellationToken cancellationToken = default)
    {
        var asset = await ResolveAssetAsync(
            query,
            subscriberProfileId,
            msisdnAssetId,
            msisdn,
            cancellationToken);

        var (iccidRaw, imsiRaw) = await MsisdnAssetKitResolver.ResolveForAssetAsync(
            query,
            asset,
            subscriberProfileId,
            operationSimInventoryId,
            cancellationToken);

        var iccid = (iccidRaw ?? "").Trim();
        var imsi = (imsiRaw ?? "").Trim();
        if (string.IsNullOrEmpty(iccid) || string.IsNullOrEmpty(imsi))
        {
            throw new InvalidOperationException("ICCID/IMSI مطلوبان لعمليات الشبكة (HLR/CBS).");
        }

        return new SubscriberLineKitContext(
            subscriberProfileId,
            asset.Id,
            asset.Msisdn,
            iccid,
            imsi,
            asset);
    }

    public static async Task<MsisdnAsset> ResolveAssetAsync(
        IQueryContext query,
        string subscriberProfileId,
        string? msisdnAssetId = null,
        string? msisdn = null,
        CancellationToken cancellationToken = default)
    {
        var canonicalMsisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(msisdn);

        var subscriptions = query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfileId == subscriberProfileId);

        if (!string.IsNullOrWhiteSpace(msisdnAssetId))
        {
            subscriptions = subscriptions.Where(s => s.MsisdnAssetId == msisdnAssetId);
        }
        else if (!string.IsNullOrEmpty(canonicalMsisdn))
        {
            subscriptions = subscriptions.Where(s =>
                s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == canonicalMsisdn);
        }

        var assetId = await subscriptions
            .OrderByDescending(s => s.IsPrimaryLine)
            .ThenByDescending(s => s.CreatedAtUtc)
            .Select(s => s.MsisdnAssetId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(assetId) && !string.IsNullOrEmpty(canonicalMsisdn))
        {
            assetId = await query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
                .Where(m => m.Msisdn == canonicalMsisdn && m.SubscriberProfileId == subscriberProfileId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(assetId))
        {
            throw new InvalidOperationException("No active MSISDN for profile.");
        }

        return await query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken)
            ?? throw new InvalidOperationException("MSISDN asset not found.");
    }
}
