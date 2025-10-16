using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.LeadContactManager.Queries;

public class GetLeadContactSingleProfile : Profile
{
    public GetLeadContactSingleProfile()
    {
    }
}

public class GetLeadContactSingleResult
{
    public LeadContact? Data { get; init; }
}

public class GetLeadContactSingleRequest : IRequest<GetLeadContactSingleResult>
{
    public string? Id { get; init; }
}

public class GetLeadContactSingleValidator : AbstractValidator<GetLeadContactSingleRequest>
{
    public GetLeadContactSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetLeadContactSingleHandler : IRequestHandler<GetLeadContactSingleRequest, GetLeadContactSingleResult>
{
    private readonly IQueryContext _context;

    public GetLeadContactSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetLeadContactSingleResult> Handle(GetLeadContactSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .LeadContact
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetLeadContactSingleResult
        {
            Data = entity
        };
    }
}