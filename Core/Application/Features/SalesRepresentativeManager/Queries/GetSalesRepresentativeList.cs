using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesRepresentativeManager.Queries;

public record GetSalesRepresentativeListDto
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

public class GetSalesRepresentativeListProfile : Profile
{
    public GetSalesRepresentativeListProfile()
    {
        CreateMap<SalesRepresentative, GetSalesRepresentativeListDto>()
            .ForMember(
                dest => dest.SalesTeamName,
                opt => opt.MapFrom(src => src.SalesTeam != null ? src.SalesTeam.Name : string.Empty)
            );
    }
}

public class GetSalesRepresentativeListResult
{
    public List<GetSalesRepresentativeListDto>? Data { get; init; }
}

public class GetSalesRepresentativeListRequest : IRequest<GetSalesRepresentativeListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetSalesRepresentativeListHandler : IRequestHandler<GetSalesRepresentativeListRequest, GetSalesRepresentativeListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetSalesRepresentativeListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetSalesRepresentativeListResult> Handle(GetSalesRepresentativeListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesRepresentative
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.SalesTeam)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetSalesRepresentativeListDto>>(entities);

        return new GetSalesRepresentativeListResult
        {
            Data = dtos
        };
    }
}