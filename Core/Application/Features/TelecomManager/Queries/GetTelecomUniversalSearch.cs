using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Telecom.UniversalSearch;
using Application.Features.TelecomManager.Queries;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public class GetTelecomUniversalSearchRequest : IRequest<GetTelecomUniversalSearchResult>, IRequirePermission
{
    public string? Term { get; init; }
    public bool AvailableOnly { get; init; }
    public bool ProfilesOnly { get; init; }
    public string PermissionKey => PermissionCatalog.CustomerView;
}

public class GetTelecomUniversalSearchHandler : IRequestHandler<GetTelecomUniversalSearchRequest, GetTelecomUniversalSearchResult>
{
    private const int MaxRows = 10;

    private readonly ITelecomUniversalSearchDataService _searchData;
    private readonly ISubscriberAccessAuditService _subscriberAudit;

    public GetTelecomUniversalSearchHandler(
        ITelecomUniversalSearchDataService searchData,
        ISubscriberAccessAuditService subscriberAudit)
    {
        _searchData = searchData;
        _subscriberAudit = subscriberAudit;
    }

    public async Task<GetTelecomUniversalSearchResult> Handle(
        GetTelecomUniversalSearchRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = TelecomUniversalSearchTerm.TryParse(request.Term);
        if (parsed == null)
        {
            return new GetTelecomUniversalSearchResult();
        }

        var results = new List<TelecomUniversalSearchRowDto>();

        if (!request.AvailableOnly)
        {
            if (parsed.SearchNationalId)
            {
                await _searchData.SearchByNationalIdAsync(results, parsed.DigitsOnly, MaxRows, cancellationToken);
            }

            if (parsed.SearchCommercialRegistry)
            {
                await _searchData.SearchByCommercialRegistryAsync(
                    results,
                    parsed.TextTerm.Trim(),
                    MaxRows,
                    cancellationToken);
            }
        }

        if (!request.ProfilesOnly && parsed.SearchMsisdn && parsed.CanonicalMsisdn != null)
        {
            await _searchData.SearchByMsisdnAsync(
                results,
                parsed.CanonicalMsisdn,
                request.AvailableOnly,
                MaxRows,
                cancellationToken);
        }

        var finalRows = TelecomUniversalSearchRowComposer.DeduplicateSearchRows(results).Take(MaxRows).ToList();

        var auditMatches = finalRows
            .GroupBy(r => r.CustomerId ?? $"row:{r.ResultType}:{r.Id}")
            .Select(g =>
            {
                var first = g.First();
                return new SubscriberSearchMatchAuditDto
                {
                    CustomerId = first.CustomerId ?? first.Id,
                    Name = first.CustomerNameAr ?? first.Title,
                    PhoneOrMsisdn = first.Msisdn ?? first.Title,
                };
            })
            .ToList();

        await _subscriberAudit.LogSearchAsync(
            "UnifiedSearch",
            null,
            null,
            request.Term,
            auditMatches,
            cancellationToken);

        return new GetTelecomUniversalSearchResult { Data = finalRows };
    }
}
