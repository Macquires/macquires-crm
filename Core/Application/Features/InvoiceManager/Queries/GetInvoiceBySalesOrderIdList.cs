using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InvoiceManager.Queries;

public record GetInvoiceBySalesOrderIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? InvoiceDate { get; init; }
    public InvoiceStatus? InvoiceStatus { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? SalesOrderId { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetInvoiceBySalesOrderIdListProfile : Profile
{
    public GetInvoiceBySalesOrderIdListProfile()
    {
        CreateMap<Invoice, GetInvoiceBySalesOrderIdListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.InvoiceStatus.HasValue ? src.InvoiceStatus.Value.ToFriendlyName() : string.Empty)
            );
    }
}

public class GetInvoiceBySalesOrderIdListResult
{
    public List<GetInvoiceBySalesOrderIdListDto>? Data { get; init; }
}

public class GetInvoiceBySalesOrderIdListRequest : IRequest<GetInvoiceBySalesOrderIdListResult>
{
    public string? SalesOrderId { get; init; }
}

public class GetInvoiceBySalesOrderIdListHandler : IRequestHandler<GetInvoiceBySalesOrderIdListRequest, GetInvoiceBySalesOrderIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetInvoiceBySalesOrderIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetInvoiceBySalesOrderIdListResult> Handle(GetInvoiceBySalesOrderIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Invoice
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Where(x => x.SalesOrderId == request.SalesOrderId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetInvoiceBySalesOrderIdListDto>>(entities);

        return new GetInvoiceBySalesOrderIdListResult
        {
            Data = dtos
        };
    }
}