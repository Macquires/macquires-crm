using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesRepresentativeManager.Queries;

public record GetSalesRepresentativeBySalesTeamIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Name { get; init; }
    public string? JobTitle { get; init; }
    public string? EmployeeNumber { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? Description { get; init; }
    public string? SalesTeamId { get; init; }
    public string? SalesTeamName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetSalesRepresentativeBySalesTeamIdListProfile : Profile
{
    public GetSalesRepresentativeBySalesTeamIdListProfile()
    {
        CreateMap<SalesRepresentative, GetSalesRepresentativeBySalesTeamIdListDto>()
            .ForMember(
                dest => dest.SalesTeamName,
                opt => opt.MapFrom(src => src.SalesTeam != null ? src.SalesTeam.Name : string.Empty)
            );
    }
}

public class GetSalesRepresentativeBySalesTeamIdListResult
{
    public List<GetSalesRepresentativeBySalesTeamIdListDto>? Data { get; init; }
}

public class GetSalesRepresentativeBySalesTeamIdListRequest : IRequest<GetSalesRepresentativeBySalesTeamIdListResult>
{
    public string? SalesTeamId { get; init; }
}

public class GetSalesRepresentativeBySalesTeamIdListHandler : IRequestHandler<GetSalesRepresentativeBySalesTeamIdListRequest, GetSalesRepresentativeBySalesTeamIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetSalesRepresentativeBySalesTeamIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetSalesRepresentativeBySalesTeamIdListResult> Handle(GetSalesRepresentativeBySalesTeamIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesRepresentative
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.SalesTeam)
            .Where(x => x.SalesTeamId == request.SalesTeamId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetSalesRepresentativeBySalesTeamIdListDto>>(entities);

        return new GetSalesRepresentativeBySalesTeamIdListResult
        {
            Data = dtos
        };
    }
}