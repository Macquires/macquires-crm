namespace Application.Common.Dashboard;

public class DashboardWidgetDataDto
{
    public string ProviderKey { get; init; } = null!;
    public string ValueText { get; init; } = "N/A";
    public decimal? ValueNumeric { get; init; }
    public string? Subtitle { get; init; }
    /// <summary>ok | warn | error</summary>
    public string Status { get; init; } = "ok";
    public IReadOnlyList<DashboardWidgetDataItemDto> Items { get; init; } = Array.Empty<DashboardWidgetDataItemDto>();
}

public record DashboardWidgetDataItemDto(string Label, string Value, string? Status = null);
