using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseRequisitionItemManager.Queries;

public record GetPurchaseRequisitionItemListDto
{
    public string? Id { get; init; }
    public string? PurchaseRequisitionId { get; init; }
    public string? PurchaseRequisitionNumber { get; init; }
    public string? VendorName { get; init; }
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductNumber { get; init; }
    public string? Summary { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public double? Total { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPurchaseRequisitionItemListProfile : Profile
{
    public GetPurchaseRequisitionItemListProfile()
    {
        CreateMap<PurchaseRequisitionItem, GetPurchaseRequisitionItemListDto>()
            .ForMember(
                dest => dest.PurchaseRequisitionNumber,
                opt => opt.MapFrom(src => src.PurchaseRequisition != null ? src.PurchaseRequisition.Number : string.Empty)
            )
            .ForMember(
                dest => dest.VendorName,
                opt => opt.MapFrom(src => src.PurchaseRequisition!.Vendor != null ? src.PurchaseRequisition.Vendor.Name : string.Empty)
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

public class GetPurchaseRequisitionItemListResult
{
    public List<GetPurchaseRequisitionItemListDto>? Data { get; init; }
}

public class GetPurchaseRequisitionItemListRequest : IRequest<GetPurchaseRequisitionItemListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetPurchaseRequisitionItemListHandler : IRequestHandler<GetPurchaseRequisitionItemListRequest, GetPurchaseRequisitionItemListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPurchaseRequisitionItemListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPurchaseRequisitionItemListResult> Handle(GetPurchaseRequisitionItemListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PurchaseRequisitionItem
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.PurchaseRequisition)
                .ThenInclude(x => x!.Vendor)
            .Include(x => x.Product)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPurchaseRequisitionItemListDto>>(entities);

        return new GetPurchaseRequisitionItemListResult
        {
            Data = dtos
        };
    }


}



