using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom;

public sealed record TechnicalTicketLineContext(
    string SubscriberProfileId,
    string Msisdn,
    string MsisdnAssetId,
    string? ProductId,
    string? CustomerId);

public static class TechnicalTicketLineResolver
{
    public static Task<TechnicalTicketLineContext> ResolveAsync(
        IQueryContext query,
        string? ticketSubscriberProfileId,
        string? ticketMsisdn,
        CancellationToken cancellationToken = default) =>
        ResolveCoreAsync(query, ticketSubscriberProfileId, ticketMsisdn, bypassBranchScope: false, cancellationToken);

    /// <summary>Cross-branch line access when ticket is in back-office queue (e.g. Tier-3 escalated).</summary>
    public static Task<TechnicalTicketLineContext> ResolveForBackOfficeTicketAsync(
        IQueryContext query,
        string? ticketSubscriberProfileId,
        string? ticketMsisdn,
        CancellationToken cancellationToken = default) =>
        ResolveCoreAsync(query, ticketSubscriberProfileId, ticketMsisdn, bypassBranchScope: true, cancellationToken);

    private static async Task<TechnicalTicketLineContext> ResolveCoreAsync(
        IQueryContext query,
        string? ticketSubscriberProfileId,
        string? ticketMsisdn,
        bool bypassBranchScope,
        CancellationToken cancellationToken)
    {
        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(ticketMsisdn)
            ?? throw new InvalidOperationException("رقم الخط غير صالح.");

        var bypass = bypassBranchScope ? query as IBranchScopeBypassQuery : null;
        var subscriptions = bypass != null
            ? bypass.TelecomSubscriptionsIgnoringBranchScope
            : query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo();
        var msisdnAssets = bypass != null
            ? bypass.MsisdnAssetsIgnoringBranchScope
            : query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo();

        if (!string.IsNullOrWhiteSpace(ticketSubscriberProfileId))
        {
            var byProfile = await subscriptions.AsNoTracking()
                .Where(s => s.SubscriberProfileId == ticketSubscriberProfileId)
                .OrderByDescending(s => s.IsPrimaryLine)
                .ThenByDescending(s => s.CreatedAtUtc)
                .Select(s => new TechnicalTicketLineContext(
                    s.SubscriberProfileId,
                    s.MsisdnAsset!.Msisdn,
                    s.MsisdnAssetId!,
                    s.ProductId,
                    s.SubscriberProfile != null ? s.SubscriberProfile.CustomerId : null))
                .FirstOrDefaultAsync(cancellationToken);

            if (byProfile != null)
            {
                return byProfile;
            }
        }

        var asset = await msisdnAssets.AsNoTracking()
            .Where(m => m.Msisdn == msisdn)
            .Select(m => new { m.Id, m.SubscriberProfileId, m.Msisdn })
            .FirstOrDefaultAsync(cancellationToken);

        if (asset != null)
        {
            var byAsset = await subscriptions.AsNoTracking()
                .Where(s => s.MsisdnAssetId == asset.Id)
                .OrderByDescending(s => s.IsPrimaryLine)
                .Select(s => new TechnicalTicketLineContext(
                    s.SubscriberProfileId,
                    asset.Msisdn,
                    s.MsisdnAssetId!,
                    s.ProductId,
                    s.SubscriberProfile != null ? s.SubscriberProfile.CustomerId : null))
                .FirstOrDefaultAsync(cancellationToken);

            if (byAsset != null)
            {
                return byAsset;
            }

            if (!string.IsNullOrEmpty(asset.SubscriberProfileId))
            {
                return new TechnicalTicketLineContext(
                    asset.SubscriberProfileId,
                    asset.Msisdn,
                    asset.Id,
                    null,
                    null);
            }
        }

        var byMsisdn = await subscriptions.AsNoTracking()
            .Where(s =>
                s.MsisdnAsset != null
                && s.MsisdnAsset.Msisdn == msisdn
                && s.MsisdnAsset.PoolStatus == MsisdnPoolStatus.Active)
            .OrderByDescending(s => s.IsPrimaryLine)
            .Select(s => new TechnicalTicketLineContext(
                s.SubscriberProfileId,
                msisdn,
                s.MsisdnAssetId!,
                s.ProductId,
                s.SubscriberProfile != null ? s.SubscriberProfile.CustomerId : null))
            .FirstOrDefaultAsync(cancellationToken);

        if (byMsisdn != null)
        {
            return byMsisdn;
        }

        throw new InvalidOperationException(
            "لا يوجد اشتراك نشط لهذا الخط. تأكد من ربط التذكرة بملف مشترك فعّال أو أعد تشغيل بذرة البيانات التجريبية.");
    }
}
