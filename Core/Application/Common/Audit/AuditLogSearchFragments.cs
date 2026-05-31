namespace Application.Common.Audit;

using Application.Common.Telecom;

/// <summary>Text fragments for audit log subscriber search (MSISDN / phone variants).</summary>
public static class AuditLogSearchFragments
{
    public static IReadOnlyList<string> Build(string? rawTerm)
    {
        var term = TelecomPhoneNormalizer.NormalizeIndicDigitsToAscii(rawTerm ?? string.Empty).Trim();
        if (term.Length < 2)
        {
            return [];
        }

        var list = new List<string> { term };

        var digitsOnly = TelecomPhoneNormalizer.DigitsOnly(term);
        if (digitsOnly.Length >= 2 && !list.Contains(digitsOnly, StringComparer.Ordinal))
        {
            list.Add(digitsOnly);
        }

        if (digitsOnly.StartsWith("9639", StringComparison.Ordinal) && digitsOnly.Length >= 12)
        {
            var local09 = "0" + digitsOnly[3..];
            if (!list.Contains(local09, StringComparer.Ordinal))
            {
                list.Add(local09);
            }
        }

        if (digitsOnly.StartsWith("009639", StringComparison.Ordinal) && digitsOnly.Length >= 14)
        {
            var local09 = "0" + digitsOnly[5..];
            if (!list.Contains(local09, StringComparer.Ordinal))
            {
                list.Add(local09);
            }
        }

        var canonical = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(term);
        if (canonical != null && !list.Contains(canonical, StringComparer.Ordinal))
        {
            list.Add(canonical);
        }

        return list.Distinct(StringComparer.Ordinal).Where(s => s.Length >= 2).Take(6).ToList();
    }
}
