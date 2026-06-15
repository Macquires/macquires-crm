using Application.Common.Telecom;

namespace Application.Common.Telecom.UniversalSearch;

public sealed record TelecomUniversalSearchTerm(
    string TextTerm,
    string DigitsOnly,
    string? CanonicalMsisdn,
    bool SearchNationalId,
    bool SearchCommercialRegistry,
    bool SearchMsisdn)
{
    public bool IsLikelyNameOnly => DigitsOnly.Length < 2 && TextTerm.Any(char.IsLetter);

    public static TelecomUniversalSearchTerm? TryParse(string? rawTerm)
    {
        var term = (rawTerm ?? string.Empty).Trim();
        if (term.Length < 2)
        {
            return null;
        }

        var textTerm = NormalizeIndicDigitsToAscii(term).Trim();
        var digitsOnly = TelecomPhoneNormalizer.DigitsOnly(textTerm);
        if (digitsOnly.Length < 2 && textTerm.Any(char.IsLetter))
        {
            return null;
        }

        var canonicalMsisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(textTerm);
        var searchNationalId = digitsOnly.Length == 10 && !LooksLikeSyrianMobileDigits(digitsOnly);
        var searchCommercialRegistry = !searchNationalId
            && canonicalMsisdn == null
            && textTerm.Length >= 3
            && textTerm.Any(char.IsLetterOrDigit);
        var searchMsisdn = canonicalMsisdn != null;

        return new TelecomUniversalSearchTerm(
            textTerm,
            digitsOnly,
            canonicalMsisdn,
            searchNationalId,
            searchCommercialRegistry,
            searchMsisdn);
    }

    public static string NormalizeIndicDigitsToAscii(string input)
    {
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];
            if (ch is >= '\u0660' and <= '\u0669')
            {
                chars[i] = (char)('0' + (ch - '\u0660'));
            }
            else if (ch is >= '\u06F0' and <= '\u06F9')
            {
                chars[i] = (char)('0' + (ch - '\u06F0'));
            }
        }

        return new string(chars);
    }

    public static string MaskNationalId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        var d = raw.Trim();
        return d.Length >= 4 ? new string('*', d.Length - 4) + d[^4..] : "****";
    }

    private static bool LooksLikeSyrianMobileDigits(string digitsOnly) =>
        digitsOnly.Length == 10 && digitsOnly.StartsWith("09", StringComparison.Ordinal);
}
