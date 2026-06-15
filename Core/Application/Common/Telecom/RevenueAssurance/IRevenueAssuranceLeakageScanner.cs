using Application.Common.Integrations;
using Domain.Entities;

namespace Application.Common.Telecom.RevenueAssurance;

public sealed record RevenueAssuranceLeakageCandidate(
    TelecomSubscription Subscription,
    HlrLiveStatusResult HlrStatus);

public interface IRevenueAssuranceLeakageScanner
{
    Task<IReadOnlyList<RevenueAssuranceLeakageCandidate>> ScanAsync(
        IReadOnlyList<TelecomSubscription> suspendedSubscriptions,
        CancellationToken cancellationToken);
}
