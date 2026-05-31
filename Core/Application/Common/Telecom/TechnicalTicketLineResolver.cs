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
    public static async Task<TechnicalTicketLineContext> ResolveAsync(
        IQueryContext query,
        string? ticketSubscriberProfileId,
        string? ticketMsisdn,
        CancellationToken cancellationToken = default)
    {
        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(ticketMsisdn)
            ?? throw new InvalidOperationException("رقم الخط غير صالح.");

        if (!string.IsNullOrWhiteSpace(ticketSubscriberProfileId))
        {
            var byProfile = await query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
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

        var asset = await query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Msisdn == msisdn)
            .Select(m => new { m.Id, m.SubscriberProfileId, m.Msisdn })
            .FirstOrDefaultAsync(cancellationToken);

        if (asset != null)
        {
            var byAsset = await query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
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

        var byMsisdn = await query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
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
