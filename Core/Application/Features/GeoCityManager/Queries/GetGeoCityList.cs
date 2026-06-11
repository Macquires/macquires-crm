using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.GeoCityManager.Queries;

public record GetGeoCityListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Governorate { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetGeoCityListProfile : Profile
{
    public GetGeoCityListProfile()
    {
        CreateMap<GeoCity, GetGeoCityListDto>();
    }
}

public class GetGeoCityListResult
{
    public List<GetGeoCityListDto>? Data { get; init; }
}

public class GetGeoCityListRequest : IRequest<GetGeoCityListResult>
{
    public bool IsDeleted { get; init; }
    public bool ActiveOnly { get; init; }
}

public class GetGeoCityListHandler : IRequestHandler<GetGeoCityListRequest, GetGeoCityListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetGeoCityListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetGeoCityListResult> Handle(GetGeoCityListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .GeoCity
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        if (request.ActiveOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var entities = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetGeoCityListDto>>(entities);

        return new GetGeoCityListResult
        {
            Data = dtos
        };
    }
}
