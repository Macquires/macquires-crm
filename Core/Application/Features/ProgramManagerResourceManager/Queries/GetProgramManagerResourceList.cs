using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProgramManagerResourceManager.Queries;

public record GetProgramManagerResourceListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetProgramManagerResourceListProfile : Profile
{
    public GetProgramManagerResourceListProfile()
    {
        CreateMap<ProgramManagerResource, GetProgramManagerResourceListDto>();
    }
}

public class GetProgramManagerResourceListResult
{
    public List<GetProgramManagerResourceListDto>? Data { get; init; }
}

public class GetProgramManagerResourceListRequest : IRequest<GetProgramManagerResourceListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetProgramManagerResourceListHandler : IRequestHandler<GetProgramManagerResourceListRequest, GetProgramManagerResourceListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetProgramManagerResourceListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetProgramManagerResourceListResult> Handle(GetProgramManagerResourceListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .ProgramManagerResource
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetProgramManagerResourceListDto>>(entities);

        return new GetProgramManagerResourceListResult
        {
            Data = dtos
        };
    }


}



