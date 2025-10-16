using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.LeadManager.Queries;

public record GetLeadByCampaignIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? CompanyName { get; init; }
    public string? CompanyAddressStreet { get; init; }
    public string? CompanyAddressCity { get; init; }
    public string? CompanyAddressState { get; init; }
    public string? CompanyPhoneNumber { get; init; }
    public string? CompanyEmail { get; init; }
    public DateTime? DateProspecting { get; init; }
    public DateTime? DateClosingEstimation { get; init; }
    public double? AmountTargeted { get; init; }
    public double? AmountClosed { get; init; }
    public double? BudgetScore { get; init; }
    public double? AuthorityScore { get; init; }
    public double? NeedScore { get; init; }
    public double? TimelineScore { get; init; }
    public string? PipelineStage { get; init; }
    public string? CampaignId { get; init; }
    public string? CampaignName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetLeadByCampaignIdListProfile : Profile
{
    public GetLeadByCampaignIdListProfile()
    {
        CreateMap<Lead, GetLeadByCampaignIdListDto>()
            .ForMember(
                dest => dest.CampaignName,
                opt => opt.MapFrom(src => src.Campaign != null ? src.Campaign.Title : string.Empty)
            )
            .ForMember(
                dest => dest.PipelineStage,
                opt => opt.MapFrom(src => src.PipelineStage.HasValue ? src.PipelineStage.Value.ToFriendlyName() : string.Empty)
            );
    }
}

public class GetLeadByCampaignIdListResult
{
    public List<GetLeadByCampaignIdListDto>? Data { get; init; }
}

public class GetLeadByCampaignIdListRequest : IRequest<GetLeadByCampaignIdListResult>
{
    public string? CampaignId { get; init; }
}

public class GetLeadByCampaignIdListHandler : IRequestHandler<GetLeadByCampaignIdListRequest, GetLeadByCampaignIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetLeadByCampaignIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetLeadByCampaignIdListResult> Handle(GetLeadByCampaignIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Lead
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.Campaign)
            .Where(x => x.CampaignId == request.CampaignId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetLeadByCampaignIdListDto>>(entities);

        return new GetLeadByCampaignIdListResult
        {
            Data = dtos
        };
    }
}