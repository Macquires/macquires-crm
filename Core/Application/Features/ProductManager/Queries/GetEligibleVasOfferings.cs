using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;

public record GetEligibleVasOfferingDto
{
    public string ServiceCode { get; init; } = "";

    public string? NameAr { get; init; }

    public string? NameEn { get; init; }

    public decimal? MonthlyFee { get; init; }

    /// <summary>When set, the VAS is also represented as a catalog <see cref="ProductOffering"/>.</summary>
    public string? ProductOfferingId { get; init; }

    public string? CatalogComponentLabel { get; init; }

    /// <summary><c>Registry</c> or <c>Catalog</c>.</summary>
    public string Source { get; init; } = "Registry";
}

public class GetEligibleVasOfferingsResult
{
    public List<GetEligibleVasOfferingDto> Data { get; init; } = new();

    public string? ResolvedSubscriptionTypeId { get; init; }

    public string? CurrentProductOfferingId { get; init; }

    public string? CurrentProductOfferingName { get; init; }
}

public class GetEligibleVasOfferingsRequest : IRequest<GetEligibleVasOfferingsResult>
{
    public string SubscriberProfileId { get; init; } = "";

    public string? MsisdnAssetId { get; init; }
}

public class GetEligibleVasOfferingsHandler
    : IRequestHandler<GetEligibleVasOfferingsRequest, GetEligibleVasOfferingsResult>
{
    private readonly IQueryContext _context;

    public GetEligibleVasOfferingsHandler(IQueryContext context) => _context = context;

    public async Task<GetEligibleVasOfferingsResult> Handle(
        GetEligibleVasOfferingsRequest request,
        CancellationToken cancellationToken)
    {
        var profileId = (request.SubscriberProfileId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(profileId))
        {
            return new GetEligibleVasOfferingsResult();
        }

        var subs = await _context.TelecomSubscription
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Where(s => s.SubscriberProfileId == profileId)
            .Include(s => s.ProductOffering)
            .ToListAsync(cancellationToken);

        if (subs.Count == 0)
        {
            return new GetEligibleVasOfferingsResult();
        }

        var msisdnId = (request.MsisdnAssetId ?? string.Empty).Trim();
        TelecomSubscription? chosen = null;
        if (!string.IsNullOrEmpty(msisdnId))
        {
            chosen = subs.FirstOrDefault(s => s.MsisdnAssetId == msisdnId);
        }

        chosen ??= subs.FirstOrDefault(s => s.IsPrimaryLine);
        chosen ??= subs[0];

        var typeId = chosen.SubscriptionTypeId;
        var now = DateTime.UtcNow;

        var registry = await _context.TelecomValueAddedService
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Where(v => v.IsActive)
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.NameAr)
            .ToListAsync(cancellationToken);

        var registryCodes = registry
            .Select(v => v.ServiceCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var catalogRows = await (
            from c in _context.ProductOfferingComponent.AsNoTracking().IsDeletedEqualTo(false)
            where c.ComponentType == ServiceComponentType.Vas
            join o in _context.ProductOffering.AsNoTracking().IsDeletedEqualTo(false) on c.ProductOfferingId equals o.Id
            where o.IsActive
                  && (o.ValidFromUtc == null || o.ValidFromUtc <= now)
                  && (o.ValidToUtc == null || o.ValidToUtc >= now)
                  && (o.CompatibleSubscriptionTypeId == null || o.CompatibleSubscriptionTypeId == typeId)
                  && (chosen.ProductOfferingId == null || o.Id != chosen.ProductOfferingId)
            join p in _context.Product.AsNoTracking().IsDeletedEqualTo(false) on o.ProductId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            where o.ProductId == null
                  || (p != null
                      && (p.CompatibleSubscriptionTypeId == null || p.CompatibleSubscriptionTypeId == typeId))
            select new
            {
                OfferingId = o.Id,
                OfferingCode = o.Code,
                ComponentLabel = c.Label,
                ProductServiceCode = p != null ? p.ServiceCode : null,
            }
        ).ToListAsync(cancellationToken);

        var catalogMeta = catalogRows
            .Select(r => new
            {
                Code = ResolveCatalogServiceCode(r.OfferingCode, r.ProductServiceCode, registryCodes),
                r.OfferingId,
                r.ComponentLabel,
            })
            .Where(x => !string.IsNullOrEmpty(x.Code))
            .GroupBy(x => x.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var data = registry.Select(v =>
        {
            catalogMeta.TryGetValue(v.ServiceCode, out var meta);
            return new GetEligibleVasOfferingDto
            {
                ServiceCode = v.ServiceCode,
                NameAr = v.NameAr,
                NameEn = v.NameEn,
                MonthlyFee = v.MonthlyFee,
                ProductOfferingId = meta?.OfferingId,
                CatalogComponentLabel = meta?.ComponentLabel,
                Source = meta != null ? "Catalog" : "Registry",
            };
        }).ToList();

        return new GetEligibleVasOfferingsResult
        {
            Data = data,
            ResolvedSubscriptionTypeId = typeId,
            CurrentProductOfferingId = chosen.ProductOfferingId,
            CurrentProductOfferingName = chosen.ProductOffering?.Name,
        };
    }

    private static string? ResolveCatalogServiceCode(
        string offeringCode,
        string? productServiceCode,
        HashSet<string> registryCodes)
    {
        if (!string.IsNullOrWhiteSpace(productServiceCode)
            && registryCodes.Contains(productServiceCode.Trim()))
        {
            return productServiceCode.Trim();
        }

        if (registryCodes.Contains(offeringCode.Trim()))
        {
            return offeringCode.Trim();
        }

        return null;
    }
}
