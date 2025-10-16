using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BillManager.Queries;

public record GetBillByPurchaseOrderIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? BillDate { get; init; }
    public BillStatus? BillStatus { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? PurchaseOrderId { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetBillByPurchaseOrderIdListProfile : Profile
{
    public GetBillByPurchaseOrderIdListProfile()
    {
        CreateMap<Bill, GetBillByPurchaseOrderIdListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.BillStatus.HasValue ? src.BillStatus.Value.ToFriendlyName() : string.Empty)
            );
    }
}

public class GetBillByPurchaseOrderIdListResult
{
    public List<GetBillByPurchaseOrderIdListDto>? Data { get; init; }
}

public class GetBillByPurchaseOrderIdListRequest : IRequest<GetBillByPurchaseOrderIdListResult>
{
    public string? PurchaseOrderId { get; init; }
}

public class GetBillByPurchaseOrderIdListHandler : IRequestHandler<GetBillByPurchaseOrderIdListRequest, GetBillByPurchaseOrderIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBillByPurchaseOrderIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBillByPurchaseOrderIdListResult> Handle(GetBillByPurchaseOrderIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Bill
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Where(x => x.PurchaseOrderId == request.PurchaseOrderId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBillByPurchaseOrderIdListDto>>(entities);

        return new GetBillByPurchaseOrderIdListResult
        {
            Data = dtos
        };
    }
}