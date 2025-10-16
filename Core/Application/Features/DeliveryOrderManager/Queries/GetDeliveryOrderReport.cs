using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DeliveryOrderManager.Queries;

public record GetDeliveryOrderReportDto
{
    public InventoryTransaction? InventoryTransaction { get; init; }
    public DeliveryOrder? DeliveryOrder { get; init; }
}

public class GetDeliveryOrderReportProfile : Profile
{
    public GetDeliveryOrderReportProfile()
    {
        CreateMap<(InventoryTransaction, DeliveryOrder?), GetDeliveryOrderReportDto>()
            .ForMember(dest => dest.InventoryTransaction, opt => opt.MapFrom(src => src.Item1))
            .ForMember(dest => dest.DeliveryOrder, opt => opt.MapFrom(src => src.Item2));
    }
}

public class GetDeliveryOrderReportResult
{
    public List<GetDeliveryOrderReportDto>? Data { get; init; }
}

public class GetDeliveryOrderReportRequest : IRequest<GetDeliveryOrderReportResult>
{
}

public class GetDeliveryOrderReportHandler : IRequestHandler<GetDeliveryOrderReportRequest, GetDeliveryOrderReportResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetDeliveryOrderReportHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetDeliveryOrderReportResult> Handle(GetDeliveryOrderReportRequest request, CancellationToken cancellationToken)
    {
        var inventoryTransactions = _context.InventoryTransaction
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .IsDeletedEqualTo(false)
            .Where(x => x.ModuleName == nameof(DeliveryOrder));

        var query = inventoryTransactions
            .GroupJoin(
                _context.DeliveryOrder
                    .AsNoTracking()
                    .Include(x => x.SalesOrder).ThenInclude(x => x.Customer),
                inventory => inventory.ModuleId,
                deliveryOrder => deliveryOrder.Id,
                (inventory, deliveryOrders) => new
                {
                    InventoryTransaction = inventory,
                    DeliveryOrders = deliveryOrders
                }
            )
            .SelectMany(
                group => group.DeliveryOrders.DefaultIfEmpty(),
                (group, deliveryOrder) => new
                {
                    group.InventoryTransaction,
                    DeliveryOrder = deliveryOrder
                }
            );

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = entities
            .Select(x => new GetDeliveryOrderReportDto
            {
                InventoryTransaction = x.InventoryTransaction,
                DeliveryOrder = x.DeliveryOrder
            })
            .ToList();

        return new GetDeliveryOrderReportResult
        {
            Data = dtos
        };
    }
}
