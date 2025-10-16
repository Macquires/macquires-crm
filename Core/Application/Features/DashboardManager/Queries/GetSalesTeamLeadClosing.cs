using Application.Common.CQS.Queries;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Queries;

public class GetSalesTeamLeadClosingResult
{
    public List<AccummulationChartItem>? Data { get; init; }
}

public class GetSalesTeamLeadClosingRequest : IRequest<GetSalesTeamLeadClosingResult>
{
}

public class GetSalesTeamLeadClosingHandler : IRequestHandler<GetSalesTeamLeadClosingRequest, GetSalesTeamLeadClosingResult>
{
    private readonly IQueryContext _context;

    public GetSalesTeamLeadClosingHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetSalesTeamLeadClosingResult> Handle(GetSalesTeamLeadClosingRequest request, CancellationToken cancellationToken)
    {
        var groupedLeads = await _context.Set<Lead>()
            .Include(l => l.SalesTeam)
            .Where(l => l.SalesTeam != null && l.AmountClosed.HasValue)
            .GroupBy(l => l.SalesTeam!.Name)
            .Select(g => new
            {
                Name = g.Key,
                TotalClosed = g.Sum(l => l.AmountClosed ?? 0)
            })
            .ToListAsync(cancellationToken);

        var totalAmountClosed = groupedLeads.Sum(g => g.TotalClosed);

        var result = groupedLeads.Select(g =>
        {
            double percentageValue = totalAmountClosed > 0
                ? (double)g.TotalClosed / totalAmountClosed * 100
                : 0;
            var percentage = percentageValue.ToString("F2");

            return new AccummulationChartItem
            {
                X = g.Name ?? "Unknown",
                Y = Math.Round(percentageValue, 2),
                Text = $"{g.Name ?? "Unknown"} ({Math.Round(percentageValue, 2)}%)"
            };
        }).ToList();

        return new GetSalesTeamLeadClosingResult { Data = result };
    }
}