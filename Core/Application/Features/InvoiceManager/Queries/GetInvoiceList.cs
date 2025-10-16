using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InvoiceManager.Queries;

public record GetInvoiceListDto
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
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetInvoiceListProfile : Profile
{
    public GetInvoiceListProfile()
    {
        CreateMap<Invoice, GetInvoiceListDto>()
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
            );
    }
}

public class GetInvoiceListResult
{
    public List<GetInvoiceListDto>? Data { get; init; }
}

public class GetInvoiceListRequest : IRequest<GetInvoiceListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetInvoiceListHandler : IRequestHandler<GetInvoiceListRequest, GetInvoiceListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetInvoiceListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetInvoiceListResult> Handle(GetInvoiceListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Invoice
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.SalesOrder)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetInvoiceListDto>>(entities);

        return new GetInvoiceListResult
        {
            Data = dtos
        };
    }
}