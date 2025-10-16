using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ExpenseManager.Queries;

public record GetExpenseByCampaignIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? ExpenseDate { get; init; }
    public ExpenseStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public double? Amount { get; init; }
    public string? CampaignId { get; init; }
    public string? CampaignName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetExpenseByCampaignIdListProfile : Profile
{
    public GetExpenseByCampaignIdListProfile()
    {
        CreateMap<Expense, GetExpenseByCampaignIdListDto>()
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

public class GetExpenseByCampaignIdListResult
{
    public List<GetExpenseByCampaignIdListDto>? Data { get; init; }
}

public class GetExpenseByCampaignIdListRequest : IRequest<GetExpenseByCampaignIdListResult>
{
    public string? CampaignId { get; init; }
}

public class GetExpenseByCampaignIdListHandler : IRequestHandler<GetExpenseByCampaignIdListRequest, GetExpenseByCampaignIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetExpenseByCampaignIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetExpenseByCampaignIdListResult> Handle(GetExpenseByCampaignIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Expense
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.Campaign)
            .Where(x => x.CampaignId == request.CampaignId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetExpenseByCampaignIdListDto>>(entities);

        return new GetExpenseByCampaignIdListResult
        {
            Data = dtos
        };
    }
}