namespace Application.Common.BulkImport;

public sealed class RowImportError
{
    public int RowNumber { get; init; }
    public string? Identifier { get; init; }
    public required string ErrorMessageAr { get; init; }
    public required string ErrorMessageEn { get; init; }
    public ParsedImportRow? Row { get; init; }
}
