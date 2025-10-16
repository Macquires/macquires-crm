using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Queries;

public class GetLeadActivityByTypeResult
{
    public List<AccummulationChartItem>? Data { get; init; }
}

public class GetLeadActivityByTypeRequest : IRequest<GetLeadActivityByTypeResult>
{
}

public class GetLeadActivityByTypeHandler : IRequestHandler<GetLeadActivityByTypeRequest, GetLeadActivityByTypeResult>
{
    private readonly IQueryContext _context;

    public GetLeadActivityByTypeHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetLeadActivityByTypeResult> Handle(GetLeadActivityByTypeRequest request, CancellationToken cancellationToken)
    {
        var groupedActivities = await _context.Set<LeadActivity>()
            .GroupBy(a => a.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderBy(g => g.Count)
            .ToListAsync(cancellationToken);

        var result = groupedActivities.Select(g => new AccummulationChartItem
        {
            X = g.Type.HasValue ? ((LeadActivityType)g.Type).ToFriendlyName() : string.Empty,
            Y = (double)g.Count,
            Text = g.Type.HasValue
                ? $"{((LeadActivityType)g.Type).ToFriendlyName()} ({g.Count})"
                : $"Unknown ({g.Count})"
        }).ToList();

        return new GetLeadActivityByTypeResult { Data = result };
    }
}