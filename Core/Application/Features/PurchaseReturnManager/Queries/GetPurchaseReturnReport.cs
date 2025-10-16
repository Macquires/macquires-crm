using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseReturnManager.Queries;

public record GetPurchaseReturnReportDto
{
    public InventoryTransaction? InventoryTransaction { get; init; }
    public PurchaseReturn? PurchaseReturn { get; init; }
}

public class GetPurchaseReturnReportProfile : Profile
{
    public GetPurchaseReturnReportProfile()
    {
        CreateMap<(InventoryTransaction, PurchaseReturn?), GetPurchaseReturnReportDto>()
            .ForMember(dest => dest.InventoryTransaction, opt => opt.MapFrom(src => src.Item1))
            .ForMember(dest => dest.PurchaseReturn, opt => opt.MapFrom(src => src.Item2));
    }
}

public class GetPurchaseReturnReportResult
{
    public List<GetPurchaseReturnReportDto>? Data { get; init; }
}

public class GetPurchaseReturnReportRequest : IRequest<GetPurchaseReturnReportResult>
{
}

public class GetPurchaseReturnReportHandler : IRequestHandler<GetPurchaseReturnReportRequest, GetPurchaseReturnReportResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPurchaseReturnReportHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPurchaseReturnReportResult> Handle(GetPurchaseReturnReportRequest request, CancellationToken cancellationToken)
    {
        var inventoryTransactions = _context.InventoryTransaction
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .IsDeletedEqualTo(false)
            .Where(x => x.ModuleName == nameof(PurchaseReturn));

        var query = inventoryTransactions
            .GroupJoin(
                _context.PurchaseReturn
                    .AsNoTracking()
                    .Include(x => x.GoodsReceive).ThenInclude(x => x.PurchaseOrder).ThenInclude(x => x.Vendor),
                inventory => inventory.ModuleId,
                purchaseReturn => purchaseReturn.Id,
                (inventory, purchaseReturns) => new
                {
                    InventoryTransaction = inventory,
                    PurchaseReturns = purchaseReturns
                }
            )
            .SelectMany(
                group => group.PurchaseReturns.DefaultIfEmpty(),
                (group, purchaseReturn) => new
                {
                    group.InventoryTransaction,
                    PurchaseReturn = purchaseReturn
                }
            );

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = entities
            .Select(x => new GetPurchaseReturnReportDto
            {
                InventoryTransaction = x.InventoryTransaction,
                PurchaseReturn = x.PurchaseReturn
            })
            .ToList();

        return new GetPurchaseReturnReportResult
        {
            Data = dtos
        };
    }
}
