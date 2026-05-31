namespace Application.Common.BulkImport;

/// <summary>One CSV row as column name → value (header-normalized keys).</summary>
public sealed class ParsedImportRow
{
    public required IReadOnlyDictionary<string, string> Columns { get; init; }

    public string? Get(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Columns.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            {
                return v.Trim();
            }
        }

        return null;
    }
}
