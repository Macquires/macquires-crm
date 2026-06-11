using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;

public record GetMigrationEligibleProductDto
{
    /// <summary><see cref="Entities.ProductOffering"/> id — same catalog key as <c>/Telecom/ProductCatalog</c>.</summary>
    public string? Id { get; init; }

    public string? Name { get; init; }

    /// <summary>Provisioning / CBS code from linked <see cref="Entities.Product"/> when present, else offering code / SOC.</summary>
    public string? ServiceCode { get; init; }

    public double? UnitPrice { get; init; }

    public string? CompatibleSubscriptionTypeId { get; init; }

    /// <summary>Optional linked technical <see cref="Entities.Product"/> for subscription rows.</summary>
    public string? ProductId { get; init; }
}

public class GetMigrationEligibleProductsResult
{
    public List<GetMigrationEligibleProductDto> Data { get; init; } = new();
    public string? ResolvedSubscriptionTypeId { get; init; }
    public string? ResolvedSubscriptionTypeLabel { get; init; }
    public string? CurrentProductName { get; init; }
}

public class GetMigrationEligibleProductsRequest : IRequest<GetMigrationEligibleProductsResult>
{
    public string SubscriberProfileId { get; init; } = "";
    public string? MsisdnAssetId { get; init; }

    /// <summary>When set (e.g. new-line activation), filters catalog by this line type instead of an existing subscription.</summary>
    public string? TargetSubscriptionTypeId { get; init; }
}

public class GetMigrationEligibleProductsHandler
    : IRequestHandler<GetMigrationEligibleProductsRequest, GetMigrationEligibleProductsResult>
{
    private readonly IQueryContext _context;

    public GetMigrationEligibleProductsHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetMigrationEligibleProductsResult> Handle(
        GetMigrationEligibleProductsRequest request,
        CancellationToken cancellationToken)
    {
        var profileId = (request.SubscriberProfileId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(profileId))
        {
            return new GetMigrationEligibleProductsResult();
        }

        var targetOverride = (request.TargetSubscriptionTypeId ?? string.Empty).Trim();
        TelecomSubscription? chosen = null;
        string typeId;
        string label;
        string? currentProductName;

        if (!string.IsNullOrEmpty(targetOverride))
        {
            typeId = targetOverride;
            var typeLookup = await _context.TelecomSubscriptionTypeLookup
                .AsNoTracking()
                .IsDeletedEqualTo(false)
                .FirstOrDefaultAsync(x => x.Id == targetOverride, cancellationToken);
            label = typeLookup != null
                ? $"{typeLookup.NameAr} ({typeLookup.NameEn})"
                : targetOverride;
            currentProductName = null;
        }
        else
        {
            var subs = await _context.TelecomSubscription
                .AsNoTracking()
                .IsDeletedEqualTo(false)
                .Where(s => s.SubscriberProfileId == profileId)
                .Include(s => s.SubscriptionTypeLookup)
                .Include(s => s.Product)
                .ToListAsync(cancellationToken);

            if (subs.Count == 0)
            {
                return new GetMigrationEligibleProductsResult();
            }

            var msisdnId = (request.MsisdnAssetId ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(msisdnId))
            {
                chosen = subs.FirstOrDefault(s => s.MsisdnAssetId == msisdnId);
            }

            chosen ??= subs.FirstOrDefault(s => s.IsPrimaryLine);
            chosen ??= subs[0];

            typeId = chosen.SubscriptionTypeId;
            var lookup = chosen.SubscriptionTypeLookup;
            label = lookup != null
                ? $"{lookup.NameAr} ({lookup.NameEn})"
                : typeId;
            currentProductName = chosen.Product?.Name ?? "باقة الدفع المسبق التلقائية (Prepaid Default Plan)";
        }

        var now = DateTime.UtcNow;

        var rows =
            from o in _context.ProductOffering.AsNoTracking().IsDeletedEqualTo(false)
            where o.IsActive
                  && (o.ValidFromUtc == null || o.ValidFromUtc <= now)
                  && (o.ValidToUtc == null || o.ValidToUtc >= now)
                  && (o.CompatibleSubscriptionTypeId == null || o.CompatibleSubscriptionTypeId == typeId)
            join p in _context.Product.AsNoTracking().IsDeletedEqualTo(false) on o.ProductId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            where o.ProductId == null
                  || (p != null
                      && (p.CompatibleSubscriptionTypeId == null || p.CompatibleSubscriptionTypeId == typeId))
            orderby o.SortOrder, o.Name
            select new GetMigrationEligibleProductDto
            {
                Id = o.Id,
                Name = o.Name,
                ServiceCode = p != null && !string.IsNullOrEmpty(p.ServiceCode)
                    ? p.ServiceCode
                    : (o.ServiceIdSocCode ?? o.Code),
                UnitPrice = p != null ? p.UnitPrice : null,
                CompatibleSubscriptionTypeId = o.CompatibleSubscriptionTypeId,
                ProductId = o.ProductId,
            };

        var offerings = await rows.ToListAsync(cancellationToken);

        return new GetMigrationEligibleProductsResult
        {
            Data = offerings,
            ResolvedSubscriptionTypeId = typeId,
            ResolvedSubscriptionTypeLabel = label,
            CurrentProductName = currentProductName,
        };
    }
}
