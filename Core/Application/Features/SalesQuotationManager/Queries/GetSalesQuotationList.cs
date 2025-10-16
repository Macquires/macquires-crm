using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesQuotationManager.Queries;

public record GetSalesQuotationListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? QuotationDate { get; init; }
    public SalesQuotationStatus? QuotationStatus { get; init; }
    public string? QuotationStatusName { get; init; }
    public string? Description { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? TaxId { get; init; }
    public string? TaxName { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetSalesQuotationListProfile : Profile
{
    public GetSalesQuotationListProfile()
    {
        CreateMap<SalesQuotation, GetSalesQuotationListDto>()
            .ForMember(
                dest => dest.CustomerName,
                opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : string.Empty)
            )
            .ForMember(
                dest => dest.TaxName,
                opt => opt.MapFrom(src => src.Tax != null ? src.Tax.Name : string.Empty)
            )
            .ForMember(
                dest => dest.QuotationStatusName,
                opt => opt.MapFrom(src => src.QuotationStatus.HasValue ? src.QuotationStatus.Value.ToFriendlyName() : string.Empty)
            );

    }
}

public class GetSalesQuotationListResult
{
    public List<GetSalesQuotationListDto>? Data { get; init; }
}

public class GetSalesQuotationListRequest : IRequest<GetSalesQuotationListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetSalesQuotationListHandler : IRequestHandler<GetSalesQuotationListRequest, GetSalesQuotationListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetSalesQuotationListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetSalesQuotationListResult> Handle(GetSalesQuotationListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesQuotation
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.Customer)
            .Include(x => x.Tax)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetSalesQuotationListDto>>(entities);

        return new GetSalesQuotationListResult
        {
            Data = dtos
        };
    }


}



