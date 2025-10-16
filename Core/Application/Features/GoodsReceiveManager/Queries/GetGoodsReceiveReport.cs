using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.GoodsReceiveManager.Queries;

public record GetGoodsReceiveReportDto
{
    public InventoryTransaction? InventoryTransaction { get; init; }
    public GoodsReceive? GoodsReceive { get; init; }
}

public class GetGoodsReceiveReportProfile : Profile
{
    public GetGoodsReceiveReportProfile()
    {
        CreateMap<(InventoryTransaction, GoodsReceive?), GetGoodsReceiveReportDto>()
            .ForMember(dest => dest.InventoryTransaction, opt => opt.MapFrom(src => src.Item1))
            .ForMember(dest => dest.GoodsReceive, opt => opt.MapFrom(src => src.Item2));
    }
}

public class GetGoodsReceiveReportResult
{
    public List<GetGoodsReceiveReportDto>? Data { get; init; }
}

public class GetGoodsReceiveReportRequest : IRequest<GetGoodsReceiveReportResult>
{
}

public class GetGoodsReceiveReportHandler : IRequestHandler<GetGoodsReceiveReportRequest, GetGoodsReceiveReportResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetGoodsReceiveReportHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetGoodsReceiveReportResult> Handle(GetGoodsReceiveReportRequest request, CancellationToken cancellationToken)
    {
        var inventoryTransactions = _context.InventoryTransaction
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .IsDeletedEqualTo(false)
            .Where(x => x.ModuleName == nameof(GoodsReceive));

        var query = inventoryTransactions
            .GroupJoin(
                _context.GoodsReceive
                    .AsNoTracking()
                    .Include(x => x.PurchaseOrder).ThenInclude(x => x.Vendor),
                inventory => inventory.ModuleId,
                goodsReceive => goodsReceive.Id,
                (inventory, goodsReceives) => new
                {
                    InventoryTransaction = inventory,
                    GoodsReceives = goodsReceives
                }
            )
            .SelectMany(
                group => group.GoodsReceives.DefaultIfEmpty(),
                (group, goodsReceive) => new
                {
                    group.InventoryTransaction,
                    GoodsReceive = goodsReceive
                }
            );

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = entities
            .Select(x => new GetGoodsReceiveReportDto
            {
                InventoryTransaction = x.InventoryTransaction,
                GoodsReceive = x.GoodsReceive
            })
            .ToList();

        return new GetGoodsReceiveReportResult
        {
            Data = dtos
        };
    }
}
