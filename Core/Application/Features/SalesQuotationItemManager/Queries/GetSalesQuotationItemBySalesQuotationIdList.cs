using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesQuotationItemManager.Queries;

public record GetSalesQuotationItemBySalesQuotationIdListDto
{
    public string? Id { get; init; }
    public string? SalesQuotationId { get; init; }
    public string? SalesQuotationNumber { get; init; }
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductNumber { get; init; }
    public string? Summary { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public double? Total { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetSalesQuotationItemBySalesQuotationIdListProfile : Profile
{
    public GetSalesQuotationItemBySalesQuotationIdListProfile()
    {
        CreateMap<SalesQuotationItem, GetSalesQuotationItemBySalesQuotationIdListDto>()
            .ForMember(
                dest => dest.SalesQuotationNumber,
                opt => opt.MapFrom(src => src.SalesQuotation != null ? src.SalesQuotation.Number : string.Empty)
            )
            .ForMember(
                dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty)
            )
            .ForMember(
                dest => dest.ProductNumber,
                opt => opt.MapFrom(src => src.Product != null ? src.Product.Number : string.Empty)
            );

    }
}

public class GetSalesQuotationItemBySalesQuotationIdListResult
{
    public List<GetSalesQuotationItemBySalesQuotationIdListDto>? Data { get; init; }
}

public class GetSalesQuotationItemBySalesQuotationIdListRequest : IRequest<GetSalesQuotationItemBySalesQuotationIdListResult>
{
    public string? SalesQuotationId { get; init; }
}


public class GetSalesQuotationItemBySalesQuotationIdListHandler : IRequestHandler<GetSalesQuotationItemBySalesQuotationIdListRequest, GetSalesQuotationItemBySalesQuotationIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetSalesQuotationItemBySalesQuotationIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetSalesQuotationItemBySalesQuotationIdListResult> Handle(GetSalesQuotationItemBySalesQuotationIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesQuotationItem
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.SalesQuotation)
            .Include(x => x.Product)
            .Where(x => x.SalesQuotationId == request.SalesQuotationId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetSalesQuotationItemBySalesQuotationIdListDto>>(entities);

        return new GetSalesQuotationItemBySalesQuotationIdListResult
        {
            Data = dtos
        };
    }


}



