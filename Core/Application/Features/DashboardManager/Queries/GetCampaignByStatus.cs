using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Queries;

public class GetCampaignByStatusResult
{
    public List<AccummulationChartItem>? Data { get; init; }
}

public class GetCampaignByStatusRequest : IRequest<GetCampaignByStatusResult>
{
}

public class GetCampaignByStatusHandler : IRequestHandler<GetCampaignByStatusRequest, GetCampaignByStatusResult>
{
    private readonly IQueryContext _context;

    public GetCampaignByStatusHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetCampaignByStatusResult> Handle(GetCampaignByStatusRequest request, CancellationToken cancellationToken)
    {
        var groupedCampaigns = await _context.Set<Campaign>()
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = groupedCampaigns.Select(g => new AccummulationChartItem
        {
            X = g.Status.HasValue ? ((CampaignStatus)g.Status).ToFriendlyName() : string.Empty,
            Y = (double)g.Count,
            Text = g.Status.HasValue
                ? $"{((CampaignStatus)g.Status).ToFriendlyName()} ({g.Count})"
                : $"Unknown ({g.Count})"
        }).ToList();

        return new GetCampaignByStatusResult { Data = result };
    }
}