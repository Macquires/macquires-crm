using Domain.Entities;

namespace Application.Common.CQS.Queries;

/// <summary>
/// Cross-branch read access for back-office technical ticket actions (HLR/CBS sync).
/// Ticket queue visibility is the authorization gate; line data may live in another branch.
/// </summary>
public interface IBranchScopeBypassQuery
{
    IQueryable<SubscriberProfile> SubscriberProfilesIgnoringBranchScope { get; }

    IQueryable<MsisdnAsset> MsisdnAssetsIgnoringBranchScope { get; }

    IQueryable<TelecomSubscription> TelecomSubscriptionsIgnoringBranchScope { get; }
}
