using Application.Common.CQS.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record TelecomUniversalSearchRowDto
{
    public string ResultType { get; init; } = null!;
    /// <summary>Entity id: SubscriberProfile id or MsisdnAsset id.</summary>
    public string Id { get; init; } = null!;
    /// <summary>Subscriber profile id when known (always set for SubscriberProfile rows; for Msisdn when linked).</summary>
    public string? SubscriberProfileId { get; init; }
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
}

public class GetTelecomUniversalSearchResult
{
    public List<TelecomUniversalSearchRowDto> Data { get; init; } = new();
}

public class GetTelecomUniversalSearchRequest : IRequest<GetTelecomUniversalSearchResult>
{
    public string? Term { get; init; }
}

public class GetTelecomUniversalSearchHandler : IRequestHandler<GetTelecomUniversalSearchRequest, GetTelecomUniversalSearchResult>
{
    private readonly IQueryContext _context;

    public GetTelecomUniversalSearchHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetTelecomUniversalSearchResult> Handle(GetTelecomUniversalSearchRequest request, CancellationToken cancellationToken)
    {
        var term = (request.Term ?? string.Empty).Trim();
        if (term.Length < 2)
            return new GetTelecomUniversalSearchResult();

        var results = new List<TelecomUniversalSearchRowDto>();

        var profiles = await _context.SubscriberProfile
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Subscriptions)
                .ThenInclude(s => s.MsisdnAsset)
            .Where(x => !x.IsDeleted && (
                (x.Customer != null && x.Customer.Name != null && x.Customer.Name.Contains(term)) ||
                (x.NationalId != null && x.NationalId.Contains(term)) ||
                (x.Customer != null && x.Customer.PhoneNumber != null && x.Customer.PhoneNumber.Replace(" ", "").Contains(term))))
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var p in profiles)
        {
            var primarySub = p.Subscriptions
                .OrderByDescending(s => s.IsPrimaryLine)
                .FirstOrDefault();
            var typeLabel = primarySub != null ? primarySub.SubscriptionType.ToString() : null;
            var subtitleParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.NationalId))
            {
                subtitleParts.Add(p.NationalId);
            }

            if (!string.IsNullOrWhiteSpace(typeLabel))
            {
                subtitleParts.Add(typeLabel);
            }

            results.Add(new TelecomUniversalSearchRowDto
            {
                ResultType = "SubscriberProfile",
                Id = p.Id,
                SubscriberProfileId = p.Id,
                Title = p.Customer?.Name,
                Subtitle = subtitleParts.Count > 0 ? string.Join(" · ", subtitleParts) : null
            });
        }

        var msisdns = await _context.MsisdnAsset
            .AsNoTracking()
            .Include(m => m.SubscriberProfile!)
                .ThenInclude(sp => sp.Subscriptions)
            .Where(x => !x.IsDeleted && x.Msisdn.Contains(term))
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var m in msisdns)
        {
            var subs = m.SubscriberProfile?.Subscriptions;
            var linked = subs?
                .OrderByDescending(s => s.MsisdnAssetId == m.Id)
                .ThenByDescending(s => s.IsPrimaryLine)
                .FirstOrDefault();
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(m.Iccid))
            {
                parts.Add(m.Iccid);
            }

            if (linked != null)
            {
                parts.Add(linked.SubscriptionType.ToString());
            }

            results.Add(new TelecomUniversalSearchRowDto
            {
                ResultType = "Msisdn",
                Id = m.Id,
                SubscriberProfileId = m.SubscriberProfileId,
                Title = m.Msisdn,
                Subtitle = parts.Count > 0 ? string.Join(" · ", parts) : m.Iccid
            });
        }

        return new GetTelecomUniversalSearchResult { Data = results };
    }
}
