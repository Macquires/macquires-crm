using Domain.Enums;

namespace Application.Common.Dashboard;

public class DashboardWidgetDefinitionDto
{
    public string Id { get; init; } = null!;
    public string WidgetKey { get; init; } = null!;
    public string TitleAr { get; init; } = null!;
    public string? TitleEn { get; init; }
    public string? Icon { get; init; }
    public string? ProviderKey { get; init; }
    public DashboardWidgetGridSize GridSize { get; init; }
    public int SortOrder { get; init; }
    public DashboardWidgetKind WidgetKind { get; init; }
    public int? RefreshIntervalSeconds { get; init; }
    public string? CtaUrl { get; init; }
    public string? CtaLabelAr { get; init; }
    public string? CtaLabelEn { get; init; }
}
