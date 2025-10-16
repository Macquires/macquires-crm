using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentReceiveManager.Queries;

public record GetPaymentReceiveReportDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Description { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? PaymentMethodId { get; init; }
    public string? PaymentMethodName { get; init; }
    public double? PaymentAmount { get; init; }
    public PaymentReceiveStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public string? InvoiceId { get; init; }
    public string? InvoiceNumber { get; init; }
    public string? CustomerName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPaymentReceiveReportProfile : Profile
{

    public GetPaymentReceiveReportProfile()
    {
        CreateMap<PaymentReceive, GetPaymentReceiveReportDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.PaymentMethodName,
                opt => opt.MapFrom(src => src.PaymentMethod != null ? src.PaymentMethod.Name : string.Empty)
            )
            .ForMember(
                dest => dest.InvoiceNumber,
                opt => opt.MapFrom(src => src.Invoice != null ? src.Invoice.Number : string.Empty)
            )
            .ForMember(
                dest => dest.CustomerName,
                opt => opt.MapFrom(src =>
                    src.Invoice != null && src.Invoice.SalesOrder != null && src.Invoice.SalesOrder.Customer != null
                        ? src.Invoice.SalesOrder.Customer.Name
                        : string.Empty
                )
            );
    }
}



public class GetPaymentReceiveReportResult
{
    public List<GetPaymentReceiveReportDto>? Data { get; init; }
}

public class GetPaymentReceiveReportRequest : IRequest<GetPaymentReceiveReportResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetPaymentReceiveReportHandler : IRequestHandler<GetPaymentReceiveReportRequest, GetPaymentReceiveReportResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPaymentReceiveReportHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPaymentReceiveReportResult> Handle(GetPaymentReceiveReportRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentReceive
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.PaymentMethod)
            .Include(x => x.Invoice)
                .ThenInclude(x => x.SalesOrder)
                    .ThenInclude(x => x.Customer)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPaymentReceiveReportDto>>(entities);

        return new GetPaymentReceiveReportResult
        {
            Data = dtos
        };
    }
}