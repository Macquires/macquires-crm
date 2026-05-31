namespace Application.Common.BulkImport;

/// <summary>Strict header mapping validation (case-insensitive, ignores spaces/underscores in names).</summary>
public static class BulkImportHeaderValidator
{
    public static string NormalizeColumnKey(string header) =>
        header.Trim().Trim('"')
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal)
            .ToLowerInvariant();

    public static BulkImportHeaderValidationResult Validate(
        IReadOnlyList<string> requiredHeaders,
        IReadOnlyList<string> fileHeaderCells)
    {
        var required = requiredHeaders
            .Select(h => (Display: h, Key: NormalizeColumnKey(h)))
            .ToList();

        var fileKeys = fileHeaderCells
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => (Raw: c.Trim(), Key: NormalizeColumnKey(c)))
            .ToList();

        var fileKeySet = fileKeys.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        var requiredKeySet = required.Select(r => r.Key).ToHashSet(StringComparer.Ordinal);

        var missing = required
            .Where(r => !fileKeySet.Contains(r.Key))
            .Select(r => r.Display)
            .ToList();

        var unexpected = fileKeys
            .Where(f => !requiredKeySet.Contains(f.Key))
            .Select(f => f.Raw)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missing.Count == 0 && unexpected.Count == 0)
        {
            return BulkImportHeaderValidationResult.Success();
        }

        return BulkImportHeaderValidationResult.Failure(missing, unexpected);
    }
}
