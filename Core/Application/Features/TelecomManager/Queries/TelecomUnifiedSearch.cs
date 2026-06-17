using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record TelecomUnifiedSearchHitDto(
    string CustomerId,
    string CustomerNameAr,
    string? NationalId,
    string? Msisdn,
    string Status);

public class TelecomUnifiedSearchResult
{
    public List<TelecomUnifiedSearchHitDto> Hits { get; init; } = new();
}

public class TelecomUnifiedSearchRequest : IRequest<TelecomUnifiedSearchResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => DashboardPermissionSets.UnifiedSearchAny;
    public string? NationalId { get; init; }
    public string? CommercialRegistrationId { get; init; }
    public string? Msisdn { get; init; }
}

public class TelecomUnifiedSearchHandler : IRequestHandler<TelecomUnifiedSearchRequest, TelecomUnifiedSearchResult>
{
    private const int MaxHits = 10;

    private readonly IQueryContext _query;
    private readonly IFieldEncryptionService _encryption;

    public TelecomUnifiedSearchHandler(IQueryContext query, IFieldEncryptionService encryption)
    {
        _query = query;
        _encryption = encryption;
    }

    private static string MaskNationalId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var d = raw.Trim();
        return d.Length >= 4 ? new string('*', d.Length - 4) + d[^4..] : "****";
    }

    public async Task<TelecomUnifiedSearchResult> Handle(
        TelecomUnifiedSearchRequest request,
        CancellationToken cancellationToken)
    {
        var hits = new List<TelecomUnifiedSearchHitDto>();

        var national = TelecomPhoneNormalizer.DigitsOnly((request.NationalId ?? "").Trim());
        if (national.Length == 10)
        {
            var hash = _encryption.ComputeSearchHash(national);
            var customers = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
                .OfType<IndividualCustomer>()
                .Where(c => c.NationalIdSearchHash == hash)
                .Select(c => new { c.Id, c.DisplayName, c.Status })
                .Take(MaxHits)
                .ToListAsync(cancellationToken);

            hits.AddRange(customers.Select(c => new TelecomUnifiedSearchHitDto(
                c.Id,
                c.DisplayName,
                MaskNationalId(national),
                null,
                c.Status.ToString())));
        }

        var registry = (request.CommercialRegistrationId ?? "").Trim();
        if (registry.Length >= 3)
        {
            var corporate = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
                .OfType<CorporateCustomer>()
                .Where(c => c.CommercialRegistryNumber == registry)
                .Select(c => new { c.Id, c.DisplayName, c.Status, c.CommercialRegistryNumber })
                .Take(MaxHits)
                .ToListAsync(cancellationToken);

            foreach (var c in corporate)
            {
                if (hits.Any(h => h.CustomerId == c.Id)) continue;
                hits.Add(new TelecomUnifiedSearchHitDto(
                    c.Id,
                    c.DisplayName,
                    c.CommercialRegistryNumber,
                    null,
                    c.Status.ToString()));
            }
        }

        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.Msisdn ?? "");
        if (!string.IsNullOrEmpty(msisdn))
        {
            var assets = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
                .Where(m => m.Msisdn == msisdn && m.SubscriberProfileId != null)
                .Select(m => new { m.Msisdn, m.SubscriberProfileId })
                .Take(MaxHits)
                .ToListAsync(cancellationToken);

            var profileIds = assets.Select(a => a.SubscriberProfileId!).Distinct().ToList();
            if (profileIds.Count > 0)
            {
                var profiles = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                    .Where(p => profileIds.Contains(p.Id))
                    .Include(p => p.Customer)
                    .Take(MaxHits)
                    .ToListAsync(cancellationToken);

                foreach (var p in profiles)
                {
                    if (p.Customer == null || hits.Any(h => h.CustomerId == p.CustomerId)) continue;
                    var assetMsisdn = assets.FirstOrDefault(a => a.SubscriberProfileId == p.Id)?.Msisdn;
                    string? idLabel = null;
                    if (p.Customer is IndividualCustomer ind)
                    {
                        idLabel = MaskNationalId(ind.NationalId);
                    }
                    else if (p.Customer is CorporateCustomer corp)
                    {
                        idLabel = corp.CommercialRegistryNumber;
                    }

                    hits.Add(new TelecomUnifiedSearchHitDto(
                        p.CustomerId,
                        p.Customer.DisplayName,
                        idLabel,
                        assetMsisdn,
                        p.Customer.Status.ToString()));
                }
            }
        }

        return new TelecomUnifiedSearchResult { Hits = hits.Take(MaxHits).ToList() };
    }
}
