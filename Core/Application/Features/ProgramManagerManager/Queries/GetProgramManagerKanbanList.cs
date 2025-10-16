using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProgramManagerManager.Queries;

public record GetProgramManagerKanbanListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Title { get; init; }
    public string? Summary { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? ProgramManagerResourceId { get; init; }
    public string? ProgramManagerResource { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetProgramManagerKanbanListProfile : Profile
{
    public GetProgramManagerKanbanListProfile()
    {
        CreateMap<ProgramManager, GetProgramManagerKanbanListDto>()
            .ForMember(
                dest => dest.ProgramManagerResource,
                opt => opt.MapFrom(src => src.ProgramManagerResource != null ? src.ProgramManagerResource.Name : string.Empty)
            )
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.Priority,
                opt => opt.MapFrom(src => src.Priority.HasValue ? src.Priority.Value.ToFriendlyName() : string.Empty)
            );

    }
}

public class GetProgramManagerKanbanListResult
{
    public List<GetProgramManagerKanbanListDto>? Data { get; init; }
}

public class GetProgramManagerKanbanListRequest : IRequest<GetProgramManagerKanbanListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetProgramManagerKanbanListHandler : IRequestHandler<GetProgramManagerKanbanListRequest, GetProgramManagerKanbanListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetProgramManagerKanbanListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetProgramManagerKanbanListResult> Handle(GetProgramManagerKanbanListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .ProgramManager
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.ProgramManagerResource)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetProgramManagerKanbanListDto>>(entities);

        return new GetProgramManagerKanbanListResult
        {
            Data = dtos
        };
    }


}



