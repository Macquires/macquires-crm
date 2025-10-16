using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InvoiceManager.Queries;

public record GetInvoiceReportDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? InvoiceDate { get; init; }
    public InvoiceStatus? InvoiceStatus { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? SalesOrderId { get; init; }
    public string? SalesOrderNumber { get; init; }
    public double? AfterTaxAmount { get; init; }
    public string? CustomerName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetInvoiceReportProfile : Profile
{
    public GetInvoiceReportProfile()
    {
        CreateMap<Invoice, GetInvoiceReportDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.InvoiceStatus.HasValue ? src.InvoiceStatus.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.SalesOrderNumber,
                opt => opt.MapFrom(src => src.SalesOrder != null ? src.SalesOrder.Number : string.Empty)
            )
            .ForMember(
                dest => dest.AfterTaxAmount,
                opt => opt.MapFrom(src => src.SalesOrder != null ? src.SalesOrder.AfterTaxAmount : 0)
            )
            .ForMember(
                dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.SalesOrder != null ? (src.SalesOrder.Customer != null ? src.SalesOrder.Customer.Name : string.Empty) : string.Empty)
            );
    }
}

public class GetInvoiceReportResult
{
    public List<GetInvoiceReportDto>? Data { get; init; }
}

public class GetInvoiceReportRequest : IRequest<GetInvoiceReportResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetInvoiceReportHandler : IRequestHandler<GetInvoiceReportRequest, GetInvoiceReportResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetInvoiceReportHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetInvoiceReportResult> Handle(GetInvoiceReportRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Invoice
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.SalesOrder)
                .ThenInclude(x => x.Customer)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetInvoiceReportDto>>(entities);

        return new GetInvoiceReportResult
        {
            Data = dtos
        };
    }
}