namespace Application.Common.Telecom.Analytics;

public sealed class ExecutiveFinancialMetrics
{
    public decimal Arpu { get; init; }
    public decimal ChurnPercent30 { get; init; }
    public decimal ChurnPercent60 { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal RevenueChangePercent { get; init; }
    public IReadOnlyList<ExecutiveBranchHeatDto> BranchHeat { get; init; } = [];
    public IReadOnlyList<ExecutiveRevenueCategoryDto> RevenueByCategory { get; init; } = [];
    public IReadOnlyList<ExecutiveRevenueTrendPointDto> RevenueTrend { get; init; } = [];
}

public sealed class ExecutiveBranchHeatDto
{
    public string BranchId { get; init; } = null!;
    public string BranchName { get; init; } = null!;
    public decimal Revenue { get; init; }
    public int ActiveSubscriptions { get; init; }
}

public sealed class ExecutiveRevenueCategoryDto
{
    public string Category { get; init; } = null!;
    public decimal Amount { get; init; }
    public decimal Percent { get; init; }
}

public sealed class ExecutiveRevenueTrendPointDto
{
    public string Label { get; init; } = null!;
    public decimal Value { get; init; }
}
