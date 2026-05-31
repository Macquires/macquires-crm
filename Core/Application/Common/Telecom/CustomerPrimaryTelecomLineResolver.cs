using Domain.Entities;

namespace Application.Common.Telecom;

/// <summary>
/// Resolves the same "primary line" as <see cref="Features.CustomerManager.Queries.GetCustomerListProfile"/>:
/// all subscriptions under all subscriber profiles, ordered by <c>IsPrimaryLine</c> descending.
/// </summary>
public static class CustomerPrimaryTelecomLineResolver
{
    public static PrimaryTelecomLineResolution? TryResolve(Customer customer)
    {
        if (customer.SubscriberProfiles == null || customer.SubscriberProfiles.Count == 0)
        {
            return null;
        }

        var ordered = customer.SubscriberProfiles
            .Where(sp => !sp.IsDeleted)
            .Where(sp => sp.Subscriptions != null)
            .SelectMany(sp => sp.Subscriptions!)
            .Where(s => s.MsisdnAsset != null)
            .OrderByDescending(s => s.IsPrimaryLine)
            .ThenBy(s => s.Id)
            .ToList();

        var sub = ordered.FirstOrDefault();
        if (sub?.MsisdnAsset == null)
        {
            return null;
        }

        return new PrimaryTelecomLineResolution(
            SubscriberProfileId: sub.SubscriberProfileId,
            Subscription: sub,
            MsisdnAsset: sub.MsisdnAsset);
    }
}

public sealed record PrimaryTelecomLineResolution(
    string SubscriberProfileId,
    TelecomSubscription Subscription,
    MsisdnAsset MsisdnAsset);
