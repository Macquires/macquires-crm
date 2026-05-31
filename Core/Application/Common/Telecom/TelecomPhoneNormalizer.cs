using System.Text;

namespace Application.Common.Telecom;

public static class TelecomPhoneNormalizer
{
    /// <summary>Maps Arabic-Indic / Eastern Arabic digits to ASCII (same idea as universal search).</summary>
    public static string NormalizeIndicDigitsToAscii(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch is >= '\u0660' and <= '\u0669')
            {
                sb.Append((char)('0' + (ch - '\u0660')));
            }
            else if (ch is >= '\u06F0' and <= '\u06F9')
            {
                sb.Append((char)('0' + (ch - '\u06F0')));
            }
            else
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    /// <summary>Extracts digits only after indic→ASCII.</summary>
    public static string DigitsOnly(string input)
    {
        var s = NormalizeIndicDigitsToAscii(input).Trim();
        return new string(s.Where(char.IsAsciiDigit).ToArray());
    }

    /// <summary>Canonical local MSISDN for Syria demo storage (09xxxxxxxxx, 10 digits).</summary>
    public static string? TryCanonicalSyrianMsisdn(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var d = DigitsOnly(raw);
        if (d.Length == 0)
        {
            return null;
        }

        if (d.StartsWith("963", StringComparison.Ordinal) && d.Length >= 12)
        {
            d = "0" + d[3..];
        }

        if (d.StartsWith("00963", StringComparison.Ordinal) && d.Length >= 14)
        {
            d = "0" + d[5..];
        }

        if (d.Length == 9 && d.StartsWith("9", StringComparison.Ordinal))
        {
            d = "0" + d;
        }

        if (d.Length == 10 && d.StartsWith("09", StringComparison.Ordinal))
        {
            return d;
        }

        if (d.Length == 9 && d[0] == '9')
        {
            return "0" + d;
        }

        return null;
    }
}
