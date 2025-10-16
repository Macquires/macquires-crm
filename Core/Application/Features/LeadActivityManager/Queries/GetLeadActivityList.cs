using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.LeadActivityManager.Queries;

public record GetLeadActivityListDto
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
    public string? LeadId { get; init; }
    public string? LeadTitle { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetLeadActivityListProfile : Profile
{
    public GetLeadActivityListProfile()
    {
        CreateMap<LeadActivity, GetLeadActivityListDto>()
            .ForMember(
                dest => dest.TypeName,
                opt => opt.MapFrom(src => src.Type.HasValue ? src.Type.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.LeadTitle,
                opt => opt.MapFrom(src => src.Lead != null ? src.Lead.Title : string.Empty)
            );
    }
}

public class GetLeadActivityListResult
{
    public List<GetLeadActivityListDto>? Data { get; init; }
}

public class GetLeadActivityListRequest : IRequest<GetLeadActivityListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetLeadActivityListHandler : IRequestHandler<GetLeadActivityListRequest, GetLeadActivityListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetLeadActivityListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetLeadActivityListResult> Handle(GetLeadActivityListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .LeadActivity
            .Include(x => x.Lead)
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetLeadActivityListDto>>(entities);

        return new GetLeadActivityListResult
        {
            Data = dtos
        };
    }
}