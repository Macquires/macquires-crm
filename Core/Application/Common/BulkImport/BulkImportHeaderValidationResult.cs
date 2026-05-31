namespace Application.Common.BulkImport;

public sealed class BulkImportHeaderValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> MissingColumns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> UnexpectedColumns { get; init; } = Array.Empty<string>();

    public string ErrorMessageAr { get; init; } = "";
    public string ErrorMessageEn { get; init; } = "";

    public static BulkImportHeaderValidationResult Success() => new() { IsValid = true };

    public static BulkImportHeaderValidationResult Failure(
        IReadOnlyList<string> missing,
        IReadOnlyList<string> unexpected)
    {
        var parts = missing.Concat(unexpected).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var joined = parts.Count > 0 ? string.Join(", ", parts) : "—";
        return new BulkImportHeaderValidationResult
        {
            IsValid = false,
            MissingColumns = missing,
            UnexpectedColumns = unexpected,
            ErrorMessageAr = $"فشل الرفع: أعمدة الملف غير مطابقة للقالب المعتمد. الأعمدة المفقودة أو الخاطئة: {joined}.",
            ErrorMessageEn = $"Upload Failed: File columns do not match the approved template. Missing or invalid columns: {joined}."
        };
    }
}
