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

    public static Task<MsisdnAsset> ResolveAssetAsync(
        IQueryContext query,
        string subscriberProfileId,
        string? msisdnAssetId = null,
        string? msisdn = null,
        CancellationToken cancellationToken = default) =>
        ResolveAssetAsync(query, subscriberProfileId, msisdnAssetId, msisdn, bypassBranchScope: false, cancellationToken);

    public static async Task<MsisdnAsset> ResolveAssetAsync(
        IQueryContext query,
        string subscriberProfileId,
        string? msisdnAssetId,
        string? msisdn,
        bool bypassBranchScope,
        CancellationToken cancellationToken)
    {
        var canonicalMsisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(msisdn);
        var bypass = bypassBranchScope ? query as IBranchScopeBypassQuery : null;

        var subscriptions = bypass != null
            ? bypass.TelecomSubscriptionsIgnoringBranchScope
            : query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo();

        subscriptions = subscriptions.AsNoTracking()
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

        var msisdnAssets = bypass != null
            ? bypass.MsisdnAssetsIgnoringBranchScope
            : query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo();

        if (string.IsNullOrEmpty(assetId) && !string.IsNullOrEmpty(canonicalMsisdn))
        {
            assetId = await msisdnAssets.AsNoTracking()
                .Where(m => m.Msisdn == canonicalMsisdn && m.SubscriberProfileId == subscriberProfileId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(assetId) && !string.IsNullOrWhiteSpace(msisdnAssetId))
        {
            assetId = msisdnAssetId;
        }

        if (string.IsNullOrEmpty(assetId))
        {
            throw new InvalidOperationException("No active MSISDN for profile.");
        }

        return await msisdnAssets.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == assetId, cancellationToken)
            ?? throw new InvalidOperationException("MSISDN asset not found.");
    }
}
