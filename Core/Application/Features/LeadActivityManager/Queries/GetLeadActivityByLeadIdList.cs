using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.LeadActivityManager.Queries;

public record GetLeadActivityByLeadIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public LeadActivityType? Type { get; init; }
    public string? TypeName { get; init; }
    public string? AttachmentName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetLeadActivityByLeadIdListProfile : Profile
{
    public GetLeadActivityByLeadIdListProfile()
    {
        CreateMap<LeadActivity, GetLeadActivityByLeadIdListDto>()
            .ForMember(
                dest => dest.TypeName,
                opt => opt.MapFrom(src => src.Type.HasValue ? src.Type.Value.ToFriendlyName() : string.Empty)
            );
    }
}

public class GetLeadActivityByLeadIdListResult
{
    public List<GetLeadActivityByLeadIdListDto>? Data { get; init; }
}

public class GetLeadActivityByLeadIdListRequest : IRequest<GetLeadActivityByLeadIdListResult>
{
    public string? LeadId { get; init; }
}

public class GetLeadActivityByLeadIdListHandler : IRequestHandler<GetLeadActivityByLeadIdListRequest, GetLeadActivityByLeadIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetLeadActivityByLeadIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetLeadActivityByLeadIdListResult> Handle(GetLeadActivityByLeadIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .LeadActivity
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Where(x => x.LeadId == request.LeadId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetLeadActivityByLeadIdListDto>>(entities);

        return new GetLeadActivityByLeadIdListResult
        {
            Data = dtos
        };
    }
}