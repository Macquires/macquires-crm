using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentDisburseManager.Queries;

public record GetPaymentDisburseReportDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Description { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? PaymentMethodId { get; init; }
    public string? PaymentMethodName { get; init; }
    public double? PaymentAmount { get; init; }
    public PaymentDisburseStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public string? BillId { get; init; }
    public string? BillNumber { get; init; }
    public string? VendorName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPaymentDisburseReportProfile : Profile
{

    public GetPaymentDisburseReportProfile()
    {
        CreateMap<PaymentDisburse, GetPaymentDisburseReportDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.PaymentMethodName,
                opt => opt.MapFrom(src => src.PaymentMethod != null ? src.PaymentMethod.Name : string.Empty)
            )
            .ForMember(
                dest => dest.BillNumber,
                opt => opt.MapFrom(src => src.Bill != null ? src.Bill.Number : string.Empty)
            )
            .ForMember(
                dest => dest.VendorName,
                opt => opt.MapFrom(src =>
                    src.Bill != null && src.Bill.PurchaseOrder != null && src.Bill.PurchaseOrder.Vendor != null
                        ? src.Bill.PurchaseOrder.Vendor.Name
                        : string.Empty
                )
            );
    }
}



public class GetPaymentDisburseReportResult
{
    public List<GetPaymentDisburseReportDto>? Data { get; init; }
}

public class GetPaymentDisburseReportRequest : IRequest<GetPaymentDisburseReportResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetPaymentDisburseReportHandler : IRequestHandler<GetPaymentDisburseReportRequest, GetPaymentDisburseReportResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPaymentDisburseReportHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPaymentDisburseReportResult> Handle(GetPaymentDisburseReportRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentDisburse
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.PaymentMethod)
            .Include(x => x.Bill)
                .ThenInclude(x => x.PurchaseOrder)
                    .ThenInclude(x => x.Vendor)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPaymentDisburseReportDto>>(entities);

        return new GetPaymentDisburseReportResult
        {
            Data = dtos
        };
    }
}