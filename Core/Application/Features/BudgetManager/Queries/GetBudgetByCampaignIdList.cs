using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BudgetManager.Queries;

public record GetBudgetByCampaignIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? BudgetDate { get; init; }
    public BudgetStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public double? Amount { get; init; }
    public string? CampaignId { get; init; }
    public string? CampaignName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetBudgetByCampaignIdListProfile : Profile
{
    public GetBudgetByCampaignIdListProfile()
    {
        CreateMap<Budget, GetBudgetByCampaignIdListDto>()
            .ForMember(
                dest => dest.CampaignName,
                opt => opt.MapFrom(src => src.Campaign != null ? src.Campaign.Title : string.Empty)
            )
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            );
    }
}

public class GetBudgetByCampaignIdListResult
{
    public List<GetBudgetByCampaignIdListDto>? Data { get; init; }
}

public class GetBudgetByCampaignIdListRequest : IRequest<GetBudgetByCampaignIdListResult>
{
    public string? CampaignId { get; init; }
}

public class GetBudgetByCampaignIdListHandler : IRequestHandler<GetBudgetByCampaignIdListRequest, GetBudgetByCampaignIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBudgetByCampaignIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBudgetByCampaignIdListResult> Handle(GetBudgetByCampaignIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Budget
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.Campaign)
            .Where(x => x.CampaignId == request.CampaignId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBudgetByCampaignIdListDto>>(entities);

        return new GetBudgetByCampaignIdListResult
        {
            Data = dtos
        };
    }
}