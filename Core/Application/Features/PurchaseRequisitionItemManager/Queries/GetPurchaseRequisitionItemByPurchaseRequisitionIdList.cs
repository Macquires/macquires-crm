using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseRequisitionItemManager.Queries;

public record GetPurchaseRequisitionItemByPurchaseRequisitionIdListDto
{
    public string? Id { get; init; }
    public string? PurchaseRequisitionId { get; init; }
    public string? PurchaseRequisitionNumber { get; init; }
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? ProductNumber { get; init; }
    public string? Summary { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public double? Total { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPurchaseRequisitionItemByPurchaseRequisitionIdListProfile : Profile
{
    public GetPurchaseRequisitionItemByPurchaseRequisitionIdListProfile()
    {
        CreateMap<PurchaseRequisitionItem, GetPurchaseRequisitionItemByPurchaseRequisitionIdListDto>()
            .ForMember(
                dest => dest.PurchaseRequisitionNumber,
                opt => opt.MapFrom(src => src.PurchaseRequisition != null ? src.PurchaseRequisition.Number : string.Empty)
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

public class GetPurchaseRequisitionItemByPurchaseRequisitionIdListResult
{
    public List<GetPurchaseRequisitionItemByPurchaseRequisitionIdListDto>? Data { get; init; }
}

public class GetPurchaseRequisitionItemByPurchaseRequisitionIdListRequest : IRequest<GetPurchaseRequisitionItemByPurchaseRequisitionIdListResult>
{
    public string? PurchaseRequisitionId { get; init; }
}


public class GetPurchaseRequisitionItemByPurchaseRequisitionIdListHandler : IRequestHandler<GetPurchaseRequisitionItemByPurchaseRequisitionIdListRequest, GetPurchaseRequisitionItemByPurchaseRequisitionIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPurchaseRequisitionItemByPurchaseRequisitionIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPurchaseRequisitionItemByPurchaseRequisitionIdListResult> Handle(GetPurchaseRequisitionItemByPurchaseRequisitionIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PurchaseRequisitionItem
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.PurchaseRequisition)
            .Include(x => x.Product)
            .Where(x => x.PurchaseRequisitionId == request.PurchaseRequisitionId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPurchaseRequisitionItemByPurchaseRequisitionIdListDto>>(entities);

        return new GetPurchaseRequisitionItemByPurchaseRequisitionIdListResult
        {
            Data = dtos
        };
    }


}



