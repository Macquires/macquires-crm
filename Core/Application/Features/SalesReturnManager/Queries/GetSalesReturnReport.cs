using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesReturnManager.Queries;

public record GetSalesReturnReportDto
{
    public InventoryTransaction? InventoryTransaction { get; init; }
    public SalesReturn? SalesReturn { get; init; }
}

public class GetSalesReturnReportProfile : Profile
{
    public GetSalesReturnReportProfile()
    {
        CreateMap<(InventoryTransaction, SalesReturn?), GetSalesReturnReportDto>()
            .ForMember(dest => dest.InventoryTransaction, opt => opt.MapFrom(src => src.Item1))
            .ForMember(dest => dest.SalesReturn, opt => opt.MapFrom(src => src.Item2));
    }
}

public class GetSalesReturnReportResult
{
    public List<GetSalesReturnReportDto>? Data { get; init; }
}

public class GetSalesReturnReportRequest : IRequest<GetSalesReturnReportResult>
{
}

public class GetSalesReturnReportHandler : IRequestHandler<GetSalesReturnReportRequest, GetSalesReturnReportResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetSalesReturnReportHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetSalesReturnReportResult> Handle(GetSalesReturnReportRequest request, CancellationToken cancellationToken)
    {
        var inventoryTransactions = _context.InventoryTransaction
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .IsDeletedEqualTo(false)
            .Where(x => x.ModuleName == nameof(SalesReturn));

        var query = inventoryTransactions
            .GroupJoin(
                _context.SalesReturn
                    .AsNoTracking()
                    .Include(x => x.DeliveryOrder).ThenInclude(x => x.SalesOrder).ThenInclude(x => x.Customer),
                inventory => inventory.ModuleId,
                salesReturn => salesReturn.Id,
                (inventory, salesReturns) => new
                {
                    InventoryTransaction = inventory,
                    SalesReturns = salesReturns
                }
            )
            .SelectMany(
                group => group.SalesReturns.DefaultIfEmpty(),
                (group, salesReturn) => new
                {
                    group.InventoryTransaction,
                    SalesReturn = salesReturn
                }
            );

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = entities
            .Select(x => new GetSalesReturnReportDto
            {
                InventoryTransaction = x.InventoryTransaction,
                SalesReturn = x.SalesReturn
            })
            .ToList();

        return new GetSalesReturnReportResult
        {
            Data = dtos
        };
    }
}
