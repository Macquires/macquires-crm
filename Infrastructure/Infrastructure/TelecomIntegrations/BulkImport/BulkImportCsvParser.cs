using System.Globalization;
using System.Text;
using Application.Common.BulkImport;

namespace Infrastructure.TelecomIntegrations.BulkImport;

internal static class BulkImportCsvParser
{
    private static readonly char[] Separators = [',', ';', '\t'];

    public static async Task<int> CountDataRowsAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var first = await reader.ReadLineAsync(cancellationToken);
        if (first == null)
        {
            return 0;
        }

        var count = 0;
        while (await reader.ReadLineAsync(cancellationToken) != null)
        {
            count++;
        }

        return count;
    }

    public static async IAsyncEnumerable<ParsedImportRow> ReadRowsAsync(
        string absolutePath,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(absolutePath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            yield break;
        }

        var headers = ParseLine(headerLine).Select(NormalizeHeader).ToArray();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseLine(line);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                var key = headers[i];
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                dict[key] = i < values.Count ? values[i] : "";
            }

            yield return new ParsedImportRow { Columns = dict };
        }
    }

    public static string NormalizeHeader(string header) =>
        BulkImportHeaderValidator.NormalizeColumnKey(header);

    public static async Task<IReadOnlyList<string>> ReadHeaderColumnsAsync(
        string absolutePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(absolutePath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return Array.Empty<string>();
        }

        return ParseLine(headerLine);
    }

    private static List<string> ParseLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (!inQuotes && (c == ',' || c == ';' || c == '\t'))
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString().Trim());
        return result;
    }
}
