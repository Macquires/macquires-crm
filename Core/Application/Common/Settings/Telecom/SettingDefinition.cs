namespace Application.Common.Settings.Telecom;

public sealed record SettingDefinition(
    string Key,
    string Category,
    SettingValueType Type,
    string DefaultValue,
    decimal? Min = null,
    decimal? Max = null,
    string? LabelAr = null,
    string? LabelEn = null);
