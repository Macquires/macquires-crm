using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CampaignManager.Queries;

public record GetCampaignListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public double? TargetRevenueAmount { get; init; }
    public DateTime? CampaignDateStart { get; init; }
    public DateTime? CampaignDateFinish { get; init; }
    public CampaignStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public string? SalesTeamId { get; init; }
    public string? SalesTeamName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetCampaignListProfile : Profile
{
    public GetCampaignListProfile()
    {
        CreateMap<Campaign, GetCampaignListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.SalesTeamName,
                opt => opt.MapFrom(src => src.SalesTeam != null ? src.SalesTeam.Name : string.Empty)
            );
    }
}

public class GetCampaignListResult
{
    public List<GetCampaignListDto>? Data { get; init; }
}

public class GetCampaignListRequest : IRequest<GetCampaignListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetCampaignListHandler : IRequestHandler<GetCampaignListRequest, GetCampaignListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetCampaignListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetCampaignListResult> Handle(GetCampaignListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Campaign
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.SalesTeam)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetCampaignListDto>>(entities);

        return new GetCampaignListResult
        {
            Data = dtos
        };
    }
}