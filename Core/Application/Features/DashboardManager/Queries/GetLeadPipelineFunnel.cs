using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Queries;

public class GetLeadPipelineFunnelResult
{
    public List<AccummulationChartItem>? Data { get; init; }
}

public class GetLeadPipelineFunnelRequest : IRequest<GetLeadPipelineFunnelResult>
{
}

public class GetLeadPipelineFunnelHandler : IRequestHandler<GetLeadPipelineFunnelRequest, GetLeadPipelineFunnelResult>
{
    private readonly IQueryContext _context;

    public GetLeadPipelineFunnelHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetLeadPipelineFunnelResult> Handle(GetLeadPipelineFunnelRequest request, CancellationToken cancellationToken)
    {
        var groupedLeads = await _context.Set<Lead>()
            .GroupBy(l => l.PipelineStage)
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = groupedLeads.Select(g => new
        {
            Item = new AccummulationChartItem
            {
                X = g.Stage.HasValue ? ((PipelineStage)g.Stage).ToFriendlyName() : string.Empty,
                Y = g.Count,
                Text = g.Stage.HasValue
                    ? $"{((PipelineStage)g.Stage).ToFriendlyName()} ({g.Count})"
                    : $"Unknown ({g.Count})"
            },
            OrderKey = g.Stage.HasValue ? ((PipelineStage)g.Stage).ToFriendlyName() : string.Empty
        })
        .OrderByDescending(i => i.OrderKey)
        .Select(i => i.Item)
        .ToList();

        return new GetLeadPipelineFunnelResult { Data = result };
    }
}