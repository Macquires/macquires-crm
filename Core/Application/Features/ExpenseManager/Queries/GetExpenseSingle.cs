using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ExpenseManager.Queries;

public class GetExpenseSingleProfile : Profile
{
    public GetExpenseSingleProfile()
    {
    }
}

public class GetExpenseSingleResult
{
    public Expense? Data { get; init; }
}

public class GetExpenseSingleRequest : IRequest<GetExpenseSingleResult>
{
    public string? Id { get; init; }
}

public class GetExpenseSingleValidator : AbstractValidator<GetExpenseSingleRequest>
{
    public GetExpenseSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetExpenseSingleHandler : IRequestHandler<GetExpenseSingleRequest, GetExpenseSingleResult>
{
    private readonly IQueryContext _context;

    public GetExpenseSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetExpenseSingleResult> Handle(GetExpenseSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Expense
            .AsNoTracking()
            .Include(x => x.Campaign)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetExpenseSingleResult
        {
            Data = entity
        };
    }
}