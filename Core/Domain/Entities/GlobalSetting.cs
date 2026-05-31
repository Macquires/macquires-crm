namespace Domain.Entities;

/// <summary>Key-value system configuration (maintenance, persona mode, integrations).</summary>
public class GlobalSetting
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = "";
    public string? Category { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }
}
