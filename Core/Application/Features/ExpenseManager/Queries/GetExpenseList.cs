using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ExpenseManager.Queries;

public record GetExpenseListDto
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

public class GetExpenseListProfile : Profile
{
    public GetExpenseListProfile()
    {
        CreateMap<Expense, GetExpenseListDto>()
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

public class GetExpenseListResult
{
    public List<GetExpenseListDto>? Data { get; init; }
}

public class GetExpenseListRequest : IRequest<GetExpenseListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetExpenseListHandler : IRequestHandler<GetExpenseListRequest, GetExpenseListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetExpenseListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetExpenseListResult> Handle(GetExpenseListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Expense
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.Campaign)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetExpenseListDto>>(entities);

        return new GetExpenseListResult
        {
            Data = dtos
        };
    }
}