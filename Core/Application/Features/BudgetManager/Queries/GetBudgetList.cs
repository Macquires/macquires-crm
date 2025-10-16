using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BudgetManager.Queries;

public record GetBudgetListDto
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

public class GetBudgetListProfile : Profile
{
    public GetBudgetListProfile()
    {
        CreateMap<Budget, GetBudgetListDto>()
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

public class GetBudgetListResult
{
    public List<GetBudgetListDto>? Data { get; init; }
}

public class GetBudgetListRequest : IRequest<GetBudgetListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetBudgetListHandler : IRequestHandler<GetBudgetListRequest, GetBudgetListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetBudgetListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetBudgetListResult> Handle(GetBudgetListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Budget
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.Campaign)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetBudgetListDto>>(entities);

        return new GetBudgetListResult
        {
            Data = dtos
        };
    }
}