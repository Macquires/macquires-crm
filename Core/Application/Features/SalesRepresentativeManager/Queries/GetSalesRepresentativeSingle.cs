using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesRepresentativeManager.Queries;

public class GetSalesRepresentativeSingleProfile : Profile
{
    public GetSalesRepresentativeSingleProfile()
    {
    }
}

public class GetSalesRepresentativeSingleResult
{
    public SalesRepresentative? Data { get; init; }
}

public class GetSalesRepresentativeSingleRequest : IRequest<GetSalesRepresentativeSingleResult>
{
    public string? Id { get; init; }
}

public class GetSalesRepresentativeSingleValidator : AbstractValidator<GetSalesRepresentativeSingleRequest>
{
    public GetSalesRepresentativeSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetSalesRepresentativeSingleHandler : IRequestHandler<GetSalesRepresentativeSingleRequest, GetSalesRepresentativeSingleResult>
{
    private readonly IQueryContext _context;

    public GetSalesRepresentativeSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetSalesRepresentativeSingleResult> Handle(GetSalesRepresentativeSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesRepresentative
            .AsNoTracking()
            .Include(x => x.SalesTeam)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetSalesRepresentativeSingleResult
        {
            Data = entity
        };
    }
}