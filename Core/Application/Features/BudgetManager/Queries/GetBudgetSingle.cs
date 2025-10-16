using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BudgetManager.Queries;

public class GetBudgetSingleProfile : Profile
{
    public GetBudgetSingleProfile()
    {
    }
}

public class GetBudgetSingleResult
{
    public Budget? Data { get; init; }
}

public class GetBudgetSingleRequest : IRequest<GetBudgetSingleResult>
{
    public string? Id { get; init; }
}

public class GetBudgetSingleValidator : AbstractValidator<GetBudgetSingleRequest>
{
    public GetBudgetSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetBudgetSingleHandler : IRequestHandler<GetBudgetSingleRequest, GetBudgetSingleResult>
{
    private readonly IQueryContext _context;

    public GetBudgetSingleHandler(
        IQueryContext context
        )
    {
        _context = context;
    }

    public async Task<GetBudgetSingleResult> Handle(GetBudgetSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Budget
            .AsNoTracking()
            .Include(x => x.Campaign)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetBudgetSingleResult
        {
            Data = entity
        };
    }
}