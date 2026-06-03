using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;

public class GetChangeGsmEligibleProductsResult
{
    public List<GetMigrationEligibleProductDto> Data { get; init; } = new();
    public string? TargetSubscriptionTypeId { get; init; }
    public string? TargetSubscriptionTypeLabel { get; init; }
}

public class GetChangeGsmEligibleProductsRequest : IRequest<GetChangeGsmEligibleProductsResult>
{
    public string SubscriberProfileId { get; init; } = "";
    public string TargetSubscriptionTypeId { get; init; } = "";
    public string? MsisdnAssetId { get; init; }
}

public class GetChangeGsmEligibleProductsHandler
    : IRequestHandler<GetChangeGsmEligibleProductsRequest, GetChangeGsmEligibleProductsResult>
{
    private readonly IQueryContext _context;

    public GetChangeGsmEligibleProductsHandler(IQueryContext context) => _context = context;

    public async Task<GetChangeGsmEligibleProductsResult> Handle(
        GetChangeGsmEligibleProductsRequest request,
        CancellationToken cancellationToken)
    {
        var profileId = (request.SubscriberProfileId ?? string.Empty).Trim();
        var targetTypeId = (request.TargetSubscriptionTypeId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(targetTypeId))
        {
            return new GetChangeGsmEligibleProductsResult();
        }

        var targetLookup = await _context.TelecomSubscriptionTypeLookup.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(t => t.Id == targetTypeId && t.IsActive, cancellationToken);

        var now = DateTime.UtcNow;

        var rows =
            from o in _context.ProductOffering.AsNoTracking().IsDeletedEqualTo(false)
            where o.IsActive
                  && (o.ValidFromUtc == null || o.ValidFromUtc <= now)
                  && (o.ValidToUtc == null || o.ValidToUtc >= now)
                  && (o.CompatibleSubscriptionTypeId == null || o.CompatibleSubscriptionTypeId == targetTypeId)
            join p in _context.Product.AsNoTracking().IsDeletedEqualTo(false) on o.ProductId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            where o.ProductId == null
                  || (p != null
                      && (p.CompatibleSubscriptionTypeId == null || p.CompatibleSubscriptionTypeId == targetTypeId))
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
        var label = targetLookup != null
            ? $"{targetLookup.NameAr} ({targetLookup.Code})"
            : targetTypeId;

        return new GetChangeGsmEligibleProductsResult
        {
            Data = offerings,
            TargetSubscriptionTypeId = targetTypeId,
            TargetSubscriptionTypeLabel = label,
        };
    }
}
