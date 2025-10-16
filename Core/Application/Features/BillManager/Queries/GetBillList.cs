using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BillManager.Queries;

public record GetBillListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? BillDate { get; init; }
    public BillStatus? BillStatus { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? PurchaseOrderNumber { get; init; }
    public double? AfterTaxAmount { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetBillListProfile : Profile
{
    public GetBillListProfile()
    {
        CreateMap<Bill, GetBillListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.BillStatus.HasValue ? src.BillStatus.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.PurchaseOrderNumber,
                opt => opt.MapFrom(src => src.PurchaseOrder != null ? src.PurchaseOrder.Number : string.Empty)
            )
            .ForMember(
                dest => dest.AfterTaxAmount,
                opt => opt.MapFrom(src => src.PurchaseOrder != null ? src.PurchaseOrder.AfterTaxAmount : 0)
            );
    }
}

public class GetBillListResult
{
    public List<GetBillListDto>? Data { get; init; }
}

public class GetBillListRequest : IRequest<GetBillListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetBillListHandler : IRequestHandler<GetBillListRequest, GetBillListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBillListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBillListResult> Handle(GetBillListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Bill
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.PurchaseOrder)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBillListDto>>(entities);

        return new GetBillListResult
        {
            Data = dtos
        };
    }
}