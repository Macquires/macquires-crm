using System.Text.Json;
using Application.Common.BulkImport;
using Application.Common.Telecom;

namespace Infrastructure.TelecomIntegrations.BulkImport;

public sealed class BulkImportErrorSanitizer : IBulkImportErrorSanitizer
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "puk1", "puk2", "puk", "pin1", "pin2", "nationalid", "nationalidnumber", "eid",
        "password", "pin", "simkey", "ki", "opc"
    };

    public string? MaskIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= 4)
        {
            return "****";
        }

        return new string('*', Math.Min(trimmed.Length - 4, 12)) + trimmed[^4..];
    }

    public string? SanitizeRowJson(ParsedImportRow? row)
    {
        if (row == null)
        {
            return null;
        }

        var safe = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, val) in row.Columns)
        {
            if (SensitiveKeys.Contains(key))
            {
                continue;
            }

            if (key.Contains("msisdn", StringComparison.OrdinalIgnoreCase)
                || key.Contains("iccid", StringComparison.OrdinalIgnoreCase)
                || key.Contains("imsi", StringComparison.OrdinalIgnoreCase))
            {
                safe[key] = MaskIdentifier(val) ?? "";
            }
            else
            {
                safe[key] = val;
            }
        }

        return JsonSerializer.Serialize(safe);
    }
}
