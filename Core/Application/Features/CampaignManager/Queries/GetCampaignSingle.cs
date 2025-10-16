using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CampaignManager.Queries;

public class GetCampaignSingleProfile : Profile
{
    public GetCampaignSingleProfile()
    {
    }
}

public class GetCampaignSingleResult
{
    public Campaign? Data { get; init; }
}

public class GetCampaignSingleRequest : IRequest<GetCampaignSingleResult>
{
    public string? Id { get; init; }
}

public class GetCampaignSingleValidator : AbstractValidator<GetCampaignSingleRequest>
{
    public GetCampaignSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetCampaignSingleHandler : IRequestHandler<GetCampaignSingleRequest, GetCampaignSingleResult>
{
    private readonly IQueryContext _context;

    public GetCampaignSingleHandler(
        IQueryContext context
        )
    {
        _context = context;
    }

    public async Task<GetCampaignSingleResult> Handle(GetCampaignSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Campaign
            .AsNoTracking()
            .Include(x => x.CampaignBudgetList.Where(budget => !budget.IsDeleted))
            .Include(x => x.CampaignExpenseList.Where(expense => !expense.IsDeleted))
            .Include(x => x.CampaignLeadList.Where(lead => !lead.IsDeleted))
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetCampaignSingleResult
        {
            Data = entity
        };
    }
}