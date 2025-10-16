using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.LeadManager.Queries;

public record GetLeadListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? CompanyName { get; init; }
    public string? CompanyDescription { get; init; }
    public string? CompanyAddressStreet { get; init; }
    public string? CompanyAddressCity { get; init; }
    public string? CompanyAddressState { get; init; }
    public string? CompanyAddressZipCode { get; init; }
    public string? CompanyAddressCountry { get; init; }
    public string? CompanyPhoneNumber { get; init; }
    public string? CompanyFaxNumber { get; init; }
    public string? CompanyEmail { get; init; }
    public string? CompanyWebsite { get; init; }
    public string? CompanyWhatsApp { get; init; }
    public string? CompanyLinkedIn { get; init; }
    public string? CompanyFacebook { get; init; }
    public string? CompanyInstagram { get; init; }
    public string? CompanyTwitter { get; init; }
    public DateTime? DateProspecting { get; init; }
    public DateTime? DateClosingEstimation { get; init; }
    public DateTime? DateClosingActual { get; init; }
    public double? AmountTargeted { get; init; }
    public double? AmountClosed { get; init; }
    public double? BudgetScore { get; init; }
    public double? AuthorityScore { get; init; }
    public double? NeedScore { get; init; }
    public double? TimelineScore { get; init; }
    public PipelineStage? PipelineStage { get; init; }
    public string? PipelineStageName { get; init; }
    public ClosingStatus? ClosingStatus { get; init; }
    public string? ClosingStatusName { get; init; }
    public string? ClosingNote { get; init; }
    public string? CampaignId { get; init; }
    public Campaign? Campaign { get; init; }
    public string? CampaignName { get; init; }
    public string? SalesTeamId { get; init; }
    public string? SalesTeamName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetLeadListProfile : Profile
{
    public GetLeadListProfile()
    {
        CreateMap<Lead, GetLeadListDto>()
            .ForMember(
                dest => dest.CampaignName,
                opt => opt.MapFrom(src => src.Campaign != null ? src.Campaign.Title : string.Empty)
            )
            .ForMember(
                dest => dest.PipelineStageName,
                opt => opt.MapFrom(src => src.PipelineStage.HasValue ? src.PipelineStage.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.ClosingStatusName,
                opt => opt.MapFrom(src => src.ClosingStatus.HasValue ? src.ClosingStatus.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.SalesTeamName,
                opt => opt.MapFrom(src => src.SalesTeam != null ? src.SalesTeam.Name : string.Empty)
            );
    }
}

public class GetLeadListResult
{
    public List<GetLeadListDto>? Data { get; init; }
}

public class GetLeadListRequest : IRequest<GetLeadListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetLeadListHandler : IRequestHandler<GetLeadListRequest, GetLeadListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetLeadListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetLeadListResult> Handle(GetLeadListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Lead
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.Campaign)
            .Include(x => x.SalesTeam)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetLeadListDto>>(entities);

        return new GetLeadListResult
        {
            Data = dtos
        };
    }
}