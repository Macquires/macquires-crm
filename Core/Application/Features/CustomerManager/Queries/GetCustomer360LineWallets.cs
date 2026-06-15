using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.Customer360;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public record Customer360UsageBucketDto(
    Domain.Enums.ServiceComponentType ComponentType,
    string? Label,
    decimal? Included,
    decimal? Remaining,
    string? Unit,
    bool IsUnlimited,
    int UsagePercent);

public record Customer360LineWalletDto(
    bool Success,
    string? Msisdn,
    decimal? Balance,
    string Currency,
    string? ErrorMessage,
    List<Customer360UsageBucketDto> Buckets,
    decimal? OutstandingBalance = null);

public class GetCustomer360LineWalletsResult
{
    public Dictionary<string, Customer360LineWalletDto> WalletsBySubscriptionId { get; init; } = new();
}

public class GetCustomer360LineWalletsRequest : IRequest<GetCustomer360LineWalletsResult>, IRequirePermission
{
    public string CustomerId { get; init; } = "";
    public string PermissionKey => PermissionCatalog.CustomerView;
}

public class GetCustomer360LineWalletsHandler : IRequestHandler<GetCustomer360LineWalletsRequest, GetCustomer360LineWalletsResult>
{
    private readonly IQueryContext _query;
    private readonly IBillingSystemIntegration _billing;

    public GetCustomer360LineWalletsHandler(IQueryContext query, IBillingSystemIntegration billing)
    {
        _query = query;
        _billing = billing;
    }

    public async Task<GetCustomer360LineWalletsResult> Handle(
        GetCustomer360LineWalletsRequest request,
        CancellationToken cancellationToken)
    {
        var profileIds = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == request.CustomerId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (profileIds.Count == 0)
            return new GetCustomer360LineWalletsResult();

        var customer = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .Where(c => c.Id == request.CustomerId)
            .Select(c => new { c.CreatedById })
            .FirstOrDefaultAsync(cancellationToken);

        var simulateDemoUsage = Customer360WalletBuilder.ShouldSimulateDemoWallet(customer?.CreatedById);

        var profiles = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => profileIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var subscriptions = Customer360SubscriptionDeduplicator.DeduplicateByLine(
            await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                .Where(s => profileIds.Contains(s.SubscriberProfileId))
                .Include(s => s.MsisdnAsset)
                .ToListAsync(cancellationToken));

        var productIds = subscriptions
            .Select(s => s.ProductId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var offeringsByProductId = productIds.Count == 0
            ? new Dictionary<string, ProductOffering>()
            : await _query.ProductOffering.AsNoTracking()
                .Where(o => !o.IsDeleted && o.ProductId != null && productIds.Contains(o.ProductId))
                .Include(o => o.Components)
                .GroupBy(o => o.ProductId!)
                .ToDictionaryAsync(g => g.Key, g => g.OrderBy(x => x.SortOrder).First(), cancellationToken);

        var walletEntries = await Task.WhenAll(subscriptions.Select(async sub =>
        {
            ProductOffering? offering = null;
            if (!string.IsNullOrEmpty(sub.ProductId))
                offeringsByProductId.TryGetValue(sub.ProductId, out offering);

            profiles.TryGetValue(sub.SubscriberProfileId, out var profile);

            var components = (offering?.Components ?? [])
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.SortOrder)
                .Select(c => new Customer360PackageComponentDto(
                    c.ComponentType,
                    c.Label,
                    c.Quota,
                    c.QuotaUnit,
                    c.IsUnlimited,
                    c.SortOrder))
                .ToList();

            var built = Customer360WalletBuilder.Build(
                sub.MsisdnAsset?.Msisdn,
                profile?.PrepaidBalance,
                components,
                simulateDemoUsage);

            decimal? outstanding = null;
            var msisdn = Customer360WalletBuilder.NormalizeMsisdn(sub.MsisdnAsset?.Msisdn);
            if (!string.IsNullOrEmpty(msisdn))
            {
                try
                {
                    outstanding = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
                }
                catch
                {
                    outstanding = null;
                }
            }

            return KeyValuePair.Create(sub.Id, built with { OutstandingBalance = outstanding });
        }));

        return new GetCustomer360LineWalletsResult
        {
            WalletsBySubscriptionId = walletEntries.ToDictionary(e => e.Key, e => e.Value),
        };
    }
}
