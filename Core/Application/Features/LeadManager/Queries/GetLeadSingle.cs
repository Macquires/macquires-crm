using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.LeadManager.Queries;

public class GetLeadSingleProfile : Profile
{
    public GetLeadSingleProfile()
    {
    }
}

public class GetLeadSingleResult
{
    public Lead? Data { get; init; }
}

public class GetLeadSingleRequest : IRequest<GetLeadSingleResult>
{
    public string? Id { get; init; }
}

public class GetLeadSingleValidator : AbstractValidator<GetLeadSingleRequest>
{
    public GetLeadSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetLeadSingleHandler : IRequestHandler<GetLeadSingleRequest, GetLeadSingleResult>
{
    private readonly IQueryContext _context;

    public GetLeadSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetLeadSingleResult> Handle(GetLeadSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Lead
            .AsNoTracking()
            .Include(x => x.Campaign)
            .Include(x => x.LeadContacts)
            .Include(x => x.LeadActivities)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetLeadSingleResult
        {
            Data = entity
        };
    }
}