using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesTeamManager.Queries;

public record GetSalesTeamListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetSalesTeamListProfile : Profile
{
    public GetSalesTeamListProfile()
    {
        CreateMap<SalesTeam, GetSalesTeamListDto>();
    }
}

public class GetSalesTeamListResult
{
    public List<GetSalesTeamListDto>? Data { get; init; }
}

public class GetSalesTeamListRequest : IRequest<GetSalesTeamListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetSalesTeamListHandler : IRequestHandler<GetSalesTeamListRequest, GetSalesTeamListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetSalesTeamListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetSalesTeamListResult> Handle(GetSalesTeamListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesTeam
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetSalesTeamListDto>>(entities);

        return new GetSalesTeamListResult
        {
            Data = dtos
        };
    }


}



