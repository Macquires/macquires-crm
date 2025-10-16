using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BillManager.Queries;

public record GetBillReportDto
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
    public string? VendorName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetBillReportProfile : Profile
{
    public GetBillReportProfile()
    {
        CreateMap<Bill, GetBillReportDto>()
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
            )
            .ForMember(
                dest => dest.VendorName,
                opt => opt.MapFrom(src => src.PurchaseOrder != null ? (src.PurchaseOrder.Vendor != null ? src.PurchaseOrder.Vendor.Name : string.Empty) : string.Empty)
            );
    }
}

public class GetBillReportResult
{
    public List<GetBillReportDto>? Data { get; init; }
}

public class GetBillReportRequest : IRequest<GetBillReportResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetBillReportHandler : IRequestHandler<GetBillReportRequest, GetBillReportResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBillReportHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBillReportResult> Handle(GetBillReportRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Bill
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.PurchaseOrder)
                .ThenInclude(x => x.Vendor)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBillReportDto>>(entities);

        return new GetBillReportResult
        {
            Data = dtos
        };
    }
}