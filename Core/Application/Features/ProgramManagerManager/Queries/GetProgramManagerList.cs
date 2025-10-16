using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProgramManagerManager.Queries;

public record GetProgramManagerListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Title { get; init; }
    public string? Summary { get; init; }
    public ProgramManagerStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public ProgramManagerPriority? Priority { get; init; }
    public string? PriorityName { get; init; }
    public string? ProgramManagerResourceId { get; init; }
    public string? ProgramManagerResourceName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetProgramManagerListProfile : Profile
{
    public GetProgramManagerListProfile()
    {
        CreateMap<ProgramManager, GetProgramManagerListDto>()
            .ForMember(
                dest => dest.ProgramManagerResourceName,
                opt => opt.MapFrom(src => src.ProgramManagerResource != null ? src.ProgramManagerResource.Name : string.Empty)
            )
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.PriorityName,
                opt => opt.MapFrom(src => src.Priority.HasValue ? src.Priority.Value.ToFriendlyName() : string.Empty)
            );

    }
}

public class GetProgramManagerListResult
{
    public List<GetProgramManagerListDto>? Data { get; init; }
}

public class GetProgramManagerListRequest : IRequest<GetProgramManagerListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetProgramManagerListHandler : IRequestHandler<GetProgramManagerListRequest, GetProgramManagerListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetProgramManagerListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetProgramManagerListResult> Handle(GetProgramManagerListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .ProgramManager
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.ProgramManagerResource)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetProgramManagerListDto>>(entities);

        return new GetProgramManagerListResult
        {
            Data = dtos
        };
    }


}



