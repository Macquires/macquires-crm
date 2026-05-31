using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Configurable Bento dashboard tile (metadata in DB; values from <see cref="ProviderKey"/> registry).</summary>
public class DashboardWidget : BaseEntity
{
    public string WidgetKey { get; set; } = null!;
    public string TitleAr { get; set; } = null!;
    public string? TitleEn { get; set; }
    public string? Icon { get; set; }
    /// <summary>Registered server-side data provider key (not a raw HTTP URL).</summary>
    public string? ProviderKey { get; set; }
    /// <summary>Comma-separated persona names, e.g. Executive,CallCenter.</summary>
    public string PersonasAllowed { get; set; } = null!;
    public DashboardWidgetGridSize GridSize { get; set; } = DashboardWidgetGridSize.Medium;
    public int SortOrder { get; set; }
    public DashboardWidgetKind WidgetKind { get; set; } = DashboardWidgetKind.Stat;
    public int? RefreshIntervalSeconds { get; set; }
    public string? CtaUrl { get; set; }
    public string? CtaLabelAr { get; set; }
    public string? CtaLabelEn { get; set; }
    public bool IsActive { get; set; } = true;
}
