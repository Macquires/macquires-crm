using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductCatalogManager.Queries;

// ─── DTOs ───
public record GetProductOfferingListDto
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
    public int ComponentCount { get; init; }
    public int PricePlanCount { get; init; }
    public decimal? DefaultPrice { get; init; }
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

    /// <summary>Optional linked technical product for CBS / subscription resolution.</summary>
    public string? ProductId { get; init; }
}

// ─── AutoMapper Profile ───
public class GetProductOfferingListProfile : Profile
{
    public GetProductOfferingListProfile()
    {
        CreateMap<ProductOffering, GetProductOfferingListDto>()
            .ForMember(
                dest => dest.CompatibleSubscriptionTypeName,
                opt => opt.MapFrom(src => src.CompatibleSubscriptionType != null ? src.CompatibleSubscriptionType.NameEn : string.Empty))
            .ForMember(
                dest => dest.ComponentCount,
                opt => opt.MapFrom(src => src.Components != null ? src.Components.Count : 0))
            .ForMember(
                dest => dest.PricePlanCount,
                opt => opt.MapFrom(src => src.PricePlans != null ? src.PricePlans.Count : 0))
            .ForMember(
                dest => dest.DefaultPrice,
                opt => opt.MapFrom(src => src.PricePlans != null
                    ? src.PricePlans.Where(pp => pp.IsDefault).Select(pp => (decimal?)pp.Price).FirstOrDefault()
                      ?? src.PricePlans.Select(pp => (decimal?)pp.Price).FirstOrDefault()
                    : null));
    }
}

// ─── Result ───
public class GetProductOfferingListResult
{
    public List<GetProductOfferingListDto>? Data { get; init; }
}

// ─── Request ───
public class GetProductOfferingListRequest : IRequest<GetProductOfferingListResult>, IRequireAnyPermission
{
    public bool IsDeleted { get; init; } = false;
    public IReadOnlyList<string> PermissionKeys => ProductCatalogPermissionSets.ReadAny;
}

// ─── Handler ───
public class GetProductOfferingListHandler : IRequestHandler<GetProductOfferingListRequest, GetProductOfferingListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetProductOfferingListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetProductOfferingListResult> Handle(GetProductOfferingListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .ProductOffering
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.CompatibleSubscriptionType)
            .Include(x => x.Components)
            .Include(x => x.PricePlans)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .AsQueryable();

        // Global Query Filter handles IsDeleted automatically.
        // For recycled items (IsDeleted=true), bypass the filter:
        if (request.IsDeleted)
        {
            query = _context
                .ProductOffering
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(x => x.IsDeleted)
                .Include(x => x.CompatibleSubscriptionType)
                .Include(x => x.Components)
                .Include(x => x.PricePlans)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name);
        }

        var entities = await query.ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetProductOfferingListDto>>(entities);

        return new GetProductOfferingListResult { Data = dtos };
    }
}
