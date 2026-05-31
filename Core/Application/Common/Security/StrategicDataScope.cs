namespace Application.Common.Security;

/// <summary>Resolved geographic boundaries for strategic analytics queries.</summary>
public sealed class StrategicDataScope
{
    public StrategicAccessLevel AccessLevel { get; init; }
    public bool CanUseFilters { get; init; }
    public string ScopeLabelAr { get; init; } = "سوريا — كامل";
    public string? EffectiveRegionId { get; init; }
    public string? EffectiveBranchId { get; init; }
    public IReadOnlyList<string> EffectiveBranchIds { get; init; } = [];
    public IReadOnlyList<StrategicScopeOptionDto> Regions { get; init; } = [];
    public IReadOnlyList<StrategicScopeOptionDto> Branches { get; init; } = [];
}

public sealed class StrategicScopeOptionDto
{
    public string Id { get; init; } = null!;
    public string NameAr { get; init; } = null!;
    public string? RegionId { get; init; }
}
