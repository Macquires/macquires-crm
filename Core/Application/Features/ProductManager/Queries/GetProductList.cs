using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;

public record GetProductListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public double? UnitPrice { get; init; }
    public bool? Physical { get; init; }
    public string? ServiceCode { get; init; }
    public string? CompatibleSubscriptionTypeId { get; init; }
    public string? CompatibleSubscriptionTypeNameAr { get; init; }
    public string? CompatibleSubscriptionTypeNameEn { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetProductListProfile : Profile
{
    public GetProductListProfile()
    {
        CreateMap<Product, GetProductListDto>()
            .ForMember(d => d.CompatibleSubscriptionTypeNameAr,
                o => o.MapFrom(s => s.CompatibleSubscriptionTypeLookup != null ? s.CompatibleSubscriptionTypeLookup.NameAr : string.Empty))
            .ForMember(d => d.CompatibleSubscriptionTypeNameEn,
                o => o.MapFrom(s => s.CompatibleSubscriptionTypeLookup != null ? s.CompatibleSubscriptionTypeLookup.NameEn : string.Empty));
    }
}

public class GetProductListResult
{
    public List<GetProductListDto>? Data { get; init; }
}

public class GetProductListRequest : IRequest<GetProductListResult>
{
    public bool IsDeleted { get; init; }
}

public class GetProductListHandler : IRequestHandler<GetProductListRequest, GetProductListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetProductListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetProductListResult> Handle(GetProductListRequest request, CancellationToken cancellationToken)
    {
        var entities = await _context.Product
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.CompatibleSubscriptionTypeLookup)
            .ToListAsync(cancellationToken);

        return new GetProductListResult { Data = _mapper.Map<List<GetProductListDto>>(entities) };
    }
}
