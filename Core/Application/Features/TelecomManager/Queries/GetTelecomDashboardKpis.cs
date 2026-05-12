using Application.Common.CQS.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class GetTelecomDashboardKpisResult
{
    public decimal ArpuDemo { get; init; }
    public decimal ChurnPercentDemo { get; init; }
    public List<GetTelecomBranchHeatDto> BranchHeat { get; init; } = new();
}

public record GetTelecomBranchHeatDto(string BranchName, decimal RevenueDemo);

public class GetTelecomDashboardKpisRequest : IRequest<GetTelecomDashboardKpisResult>
{
}

/// <summary>Demo KPIs derived from existing sales data (see docs/TELECOM_MIS_DEFINITIONS.md).</summary>
public class GetTelecomDashboardKpisHandler : IRequestHandler<GetTelecomDashboardKpisRequest, GetTelecomDashboardKpisResult>
{
    private readonly IQueryContext _context;

    public GetTelecomDashboardKpisHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetTelecomDashboardKpisResult> Handle(GetTelecomDashboardKpisRequest request, CancellationToken cancellationToken)
    {
        var orders = await _context.SalesOrder
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.Customer)
            .ToListAsync(cancellationToken);

        var count = orders.Select(x => x.CustomerId).Distinct().Count();
        var total = orders.Sum(x => (decimal)(x.AfterTaxAmount ?? 0));
        var arpu = count > 0 ? total / count : 0m;

        var branchHeat = orders
            .Where(x => x.Customer != null && !string.IsNullOrEmpty(x.Customer.City))
            .GroupBy(x => x.Customer!.City!)
            .Select(g => new GetTelecomBranchHeatDto(g.Key, (decimal)g.Sum(x => x.AfterTaxAmount ?? 0)))
            .OrderByDescending(x => x.RevenueDemo)
            .Take(8)
            .ToList();

        if (!branchHeat.Any())
        {
            branchHeat =
            [
                new GetTelecomBranchHeatDto("Damascus", 125000m),
                new GetTelecomBranchHeatDto("Aleppo", 98000m),
                new GetTelecomBranchHeatDto("Homs", 72000m),
                new GetTelecomBranchHeatDto("Lattakia", 61000m)
            ];
        }

        return new GetTelecomDashboardKpisResult
        {
            ArpuDemo = decimal.Round(arpu, 2),
            ChurnPercentDemo = 2.1m,
            BranchHeat = branchHeat
        };
    }
}
