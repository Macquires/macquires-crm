using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesQuotationItemManager.Queries;

public record GetSalesQuotationItemListDto
{
    public string? Id { get; init; }
    public string? SalesQuotationId { get; init; }
    public string? SalesQuotationNumber { get; init; }
    public string? CustomerName { get; init; }
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductNumber { get; init; }
    public string? Summary { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public double? Total { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetSalesQuotationItemListProfile : Profile
{
    public GetSalesQuotationItemListProfile()
    {
        CreateMap<SalesQuotationItem, GetSalesQuotationItemListDto>()
            .ForMember(
                dest => dest.SalesQuotationNumber,
                opt => opt.MapFrom(src => src.SalesQuotation != null ? src.SalesQuotation.Number : string.Empty)
            )
            .ForMember(
                dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.SalesQuotation!.Customer != null ? src.SalesQuotation.Customer.Name : string.Empty)
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

public class GetSalesQuotationItemListResult
{
    public List<GetSalesQuotationItemListDto>? Data { get; init; }
}

public class GetSalesQuotationItemListRequest : IRequest<GetSalesQuotationItemListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetSalesQuotationItemListHandler : IRequestHandler<GetSalesQuotationItemListRequest, GetSalesQuotationItemListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetSalesQuotationItemListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetSalesQuotationItemListResult> Handle(GetSalesQuotationItemListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesQuotationItem
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.SalesQuotation)
                .ThenInclude(x => x!.Customer)
            .Include(x => x.Product)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetSalesQuotationItemListDto>>(entities);

        return new GetSalesQuotationItemListResult
        {
            Data = dtos
        };
    }


}



