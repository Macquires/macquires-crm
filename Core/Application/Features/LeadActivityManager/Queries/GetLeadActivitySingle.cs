using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.LeadActivityManager.Queries;

public class GetLeadActivitySingleProfile : Profile
{
    public GetLeadActivitySingleProfile()
    {
    }
}

public class GetLeadActivitySingleResult
{
    public LeadActivity? Data { get; init; }
}

public class GetLeadActivitySingleRequest : IRequest<GetLeadActivitySingleResult>
{
    public string? Id { get; init; }
}

public class GetLeadActivitySingleValidator : AbstractValidator<GetLeadActivitySingleRequest>
{
    public GetLeadActivitySingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetLeadActivitySingleHandler : IRequestHandler<GetLeadActivitySingleRequest, GetLeadActivitySingleResult>
{
    private readonly IQueryContext _context;

    public GetLeadActivitySingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetLeadActivitySingleResult> Handle(GetLeadActivitySingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .LeadActivity
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetLeadActivitySingleResult
        {
            Data = entity
        };
    }
}