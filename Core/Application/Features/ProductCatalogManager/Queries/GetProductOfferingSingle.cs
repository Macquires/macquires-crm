using Application.Common.CQS.Queries;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductCatalogManager.Queries;

// ─── DTOs ───
public record ProductOfferingComponentDto
{
    public string? Id { get; init; }
    public ServiceComponentType ComponentType { get; init; }
    public string? Label { get; init; }
    public decimal? Quota { get; init; }
    public string? QuotaUnit { get; init; }
    public bool IsUnlimited { get; init; }
    public int SortOrder { get; init; }
}

public record PricePlanDto
{
    public string? Id { get; init; }
    public PricePlanType PlanType { get; init; }
    public decimal Price { get; init; }
    public string? CurrencyCode { get; init; }
    public decimal? ActivationFee { get; init; }
    public int? ValidityDays { get; init; }
    public bool IsDefault { get; init; }
}

// ─── Result ───
public class GetProductOfferingSingleResult
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? NameEn { get; init; }
    public string? Description { get; init; }
    public string? Code { get; init; }
    public string? CompatibleSubscriptionTypeId { get; init; }
    public string? CompatibleSubscriptionTypeName { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidToUtc { get; init; }
    public int SortOrder { get; init; }
    public DateTime? CreatedAtUtc { get; init; }

    public string? EligibilityRules { get; init; }
    public string? AssetCompatibility { get; init; }
    public string? BillingCycle { get; init; }
    public string? TaxCategory { get; init; }
    public string? ServiceIdSocCode { get; init; }
    public double? SpeedQuotaLimitGb { get; init; }
    public int? VoiceMinutesLimit { get; init; }
    public string? ThrottlingPolicy { get; init; }
    public string? IconClass { get; init; }
    public string? BadgeColor { get; init; }
    public string? ShortDescription { get; init; }

    public string? ProductId { get; init; }

    public List<ProductOfferingComponentDto> Components { get; init; } = new();
    public List<PricePlanDto> PricePlans { get; init; } = new();
}

// ─── Request ───
public class GetProductOfferingSingleRequest : IRequest<GetProductOfferingSingleResult?>
{
    public string Id { get; init; } = null!;
}

// ─── Handler ───
public class GetProductOfferingSingleHandler : IRequestHandler<GetProductOfferingSingleRequest, GetProductOfferingSingleResult?>
{
    private readonly IQueryContext _context;

    public GetProductOfferingSingleHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetProductOfferingSingleResult?> Handle(GetProductOfferingSingleRequest request, CancellationToken cancellationToken)
    {
        var entity = await _context
            .ProductOffering
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.CompatibleSubscriptionType)
            .Include(x => x.Components.OrderBy(c => c.SortOrder))
            .Include(x => x.PricePlans.OrderByDescending(pp => pp.IsDefault))
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
            return null;

        return new GetProductOfferingSingleResult
        {
            Id = entity.Id,
            Name = entity.Name,
            NameEn = entity.NameEn,
            Description = entity.Description,
            Code = entity.Code,
            CompatibleSubscriptionTypeId = entity.CompatibleSubscriptionTypeId,
            CompatibleSubscriptionTypeName = entity.CompatibleSubscriptionType?.NameEn,
            IsActive = entity.IsActive,
            ValidFromUtc = entity.ValidFromUtc,
            ValidToUtc = entity.ValidToUtc,
            SortOrder = entity.SortOrder,
            CreatedAtUtc = entity.CreatedAtUtc,

            EligibilityRules = entity.EligibilityRules,
            AssetCompatibility = entity.AssetCompatibility,
            BillingCycle = entity.BillingCycle,
            TaxCategory = entity.TaxCategory,
            ServiceIdSocCode = entity.ServiceIdSocCode,
            SpeedQuotaLimitGb = entity.SpeedQuotaLimitGb,
            VoiceMinutesLimit = entity.VoiceMinutesLimit,
            ThrottlingPolicy = entity.ThrottlingPolicy,
            IconClass = entity.IconClass,
            BadgeColor = entity.BadgeColor,
            ShortDescription = entity.ShortDescription,
            ProductId = entity.ProductId,

            Components = entity.Components.Select(c => new ProductOfferingComponentDto
            {
                Id = c.Id,
                ComponentType = c.ComponentType,
                Label = c.Label,
                Quota = c.Quota,
                QuotaUnit = c.QuotaUnit,
                IsUnlimited = c.IsUnlimited,
                SortOrder = c.SortOrder
            }).ToList(),
            PricePlans = entity.PricePlans.Select(pp => new PricePlanDto
            {
                Id = pp.Id,
                PlanType = pp.PlanType,
                Price = pp.Price,
                CurrencyCode = pp.CurrencyCode,
                ActivationFee = pp.ActivationFee,
                ValidityDays = pp.ValidityDays,
                IsDefault = pp.IsDefault
            }).ToList()
        };
    }
}
