namespace Infrastructure.TelecomIntegrations.BulkImport;

public sealed class BulkImportOptions
{
    public const string SectionName = "BulkImport";

    public string PathFolder { get; set; } = "wwwroot/app_data/bulk-imports";

    public int MaxFileSizeMb { get; set; } = 50;

    public int BatchSize { get; set; } = 1000;

    public int StaleProcessingMinutes { get; set; } = 30;

    public int InlineEnqueueMaxRows { get; set; } = 500;
}
