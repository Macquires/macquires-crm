using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseRequisitionManager.Queries;

public record GetPurchaseRequisitionListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? RequisitionDate { get; init; }
    public PurchaseRequisitionStatus? RequisitionStatus { get; init; }
    public string? RequisitionStatusName { get; init; }
    public string? Description { get; init; }
    public string? VendorId { get; init; }
    public string? VendorName { get; init; }
    public string? TaxId { get; init; }
    public string? TaxName { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPurchaseRequisitionListProfile : Profile
{
    public GetPurchaseRequisitionListProfile()
    {
        CreateMap<PurchaseRequisition, GetPurchaseRequisitionListDto>()
            .ForMember(
                dest => dest.VendorName,
                opt => opt.MapFrom(src => src.Vendor != null ? src.Vendor.Name : string.Empty)
            )
            .ForMember(
                dest => dest.TaxName,
                opt => opt.MapFrom(src => src.Tax != null ? src.Tax.Name : string.Empty)
            )
            .ForMember(
                dest => dest.RequisitionStatusName,
                opt => opt.MapFrom(src => src.RequisitionStatus.HasValue ? src.RequisitionStatus.Value.ToFriendlyName() : string.Empty)
            );

    }
}

public class GetPurchaseRequisitionListResult
{
    public List<GetPurchaseRequisitionListDto>? Data { get; init; }
}

public class GetPurchaseRequisitionListRequest : IRequest<GetPurchaseRequisitionListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetPurchaseRequisitionListHandler : IRequestHandler<GetPurchaseRequisitionListRequest, GetPurchaseRequisitionListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPurchaseRequisitionListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPurchaseRequisitionListResult> Handle(GetPurchaseRequisitionListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PurchaseRequisition
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.Vendor)
            .Include(x => x.Tax)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPurchaseRequisitionListDto>>(entities);

        return new GetPurchaseRequisitionListResult
        {
            Data = dtos
        };
    }


}



