using Domain.Entities;

namespace Application.Common.Telecom.Customer360;

public static class Customer360SubscriptionDeduplicator
{
    /// <summary>
    /// One logical line per MSISDN asset — demo/reconnect seeders may leave stale subscription rows on old profiles.
    /// </summary>
    public static List<TelecomSubscription> DeduplicateByLine(IReadOnlyList<TelecomSubscription> subscriptions)
    {
        if (subscriptions.Count <= 1)
        {
            return subscriptions.ToList();
        }

        return subscriptions
            .GroupBy(s => !string.IsNullOrEmpty(s.MsisdnAssetId) ? s.MsisdnAssetId : s.Id)
            .Select(g =>
            {
                var canonicalProfileId = g
                    .Select(x => x.MsisdnAsset?.SubscriberProfileId)
                    .FirstOrDefault(id => !string.IsNullOrEmpty(id));

                var candidates = !string.IsNullOrEmpty(canonicalProfileId)
                    ? g.Where(s => s.SubscriberProfileId == canonicalProfileId).ToList()
                    : g.ToList();

                return candidates
                    .OrderByDescending(s => s.IsPrimaryLine)
                    .ThenByDescending(s => s.CreatedAtUtc ?? DateTime.MinValue)
                    .First();
            })
            .OrderByDescending(s => s.IsPrimaryLine)
            .ThenBy(s => s.CreatedAtUtc)
            .ToList();
    }
}
