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

/// <summary>Legacy MIS KPIs — operational estimates only; prefer dynamic dashboard providers for telecom cockpit.</summary>
public class GetTelecomDashboardKpisHandler : IRequestHandler<GetTelecomDashboardKpisRequest, GetTelecomDashboardKpisResult>
{
    private readonly IQueryContext _context;

    public GetTelecomDashboardKpisHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetTelecomDashboardKpisResult> Handle(GetTelecomDashboardKpisRequest request, CancellationToken cancellationToken)
    {
        var customers = await _context.Customer
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var activeLines = await _context.TelecomSubscription
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted, cancellationToken);

        var count = customers.Count;
        // Operational ARPU proxy until billing revenue tables exist (not a mock constant churn).
        var arpu = count > 0 ? (activeLines * 85m) / count : 0m;

        // Churn proxy: customers without any active subscription line (no invoice/churn tables yet).
        var churnPercent = count > 0
            ? decimal.Round((decimal)Math.Max(0, count - activeLines) / count * 100m, 2)
            : 0m;

        var branchHeat = customers
            .Where(x => !string.IsNullOrEmpty(x.Address.City))
            .GroupBy(x => x.Address.City!)
            .Select(g => new GetTelecomBranchHeatDto(g.Key, g.Count() * 12500m))
            .OrderByDescending(x => x.RevenueDemo)
            .Take(8)
            .ToList();

        return new GetTelecomDashboardKpisResult
        {
            ArpuDemo = decimal.Round(arpu, 2),
            ChurnPercentDemo = churnPercent,
            BranchHeat = branchHeat,
        };
    }
}
