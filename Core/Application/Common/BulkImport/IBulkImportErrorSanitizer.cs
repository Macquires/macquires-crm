namespace Application.Common.BulkImport;

public interface IBulkImportErrorSanitizer
{
    string? MaskIdentifier(string? value);

    string? SanitizeRowJson(ParsedImportRow? row);
}
