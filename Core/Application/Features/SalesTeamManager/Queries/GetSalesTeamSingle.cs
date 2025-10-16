using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesTeamManager.Queries;

public record GetSalesTeamSingleDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}

public class GetSalesTeamSingleProfile : Profile
{
    public GetSalesTeamSingleProfile()
    {
        CreateMap<SalesTeam, GetSalesTeamSingleDto>();
    }
}

public class GetSalesTeamSingleResult
{
    public GetSalesTeamSingleDto? Data { get; init; }
}

public class GetSalesTeamSingleRequest : IRequest<GetSalesTeamSingleResult>
{
    public string? Id { get; init; }
}

public class GetSalesTeamSingleValidator : AbstractValidator<GetSalesTeamSingleRequest>
{
    public GetSalesTeamSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetSalesTeamSingleHandler : IRequestHandler<GetSalesTeamSingleRequest, GetSalesTeamSingleResult>
{
    private readonly IQueryContext _context;
    private readonly IMapper _mapper;

    public GetSalesTeamSingleHandler(
        IQueryContext context,
        IMapper mapper
        )
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetSalesTeamSingleResult> Handle(GetSalesTeamSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesTeam
            .AsNoTracking()
            .AsQueryable();

        query = query
            .Where(x => x.Id == request.Id);

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        var dto = _mapper.Map<GetSalesTeamSingleDto>(entity);

        return new GetSalesTeamSingleResult
        {
            Data = dto
        };
    }
}