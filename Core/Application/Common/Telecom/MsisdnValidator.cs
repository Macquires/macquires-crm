using System.Text.RegularExpressions;

namespace Application.Common.Telecom;

/// <summary>Structural MSISDN guardrails for Syrian demo network (09xxxxxxxx).</summary>
public static partial class MsisdnValidator
{
    private static readonly Regex SyrianLocalPattern = SyrianMsisdnRegex();

    public static bool TryValidate(string? raw, out string normalized, out string? error)
    {
        normalized = "";
        error = null;

        var canonical = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(raw);
        if (canonical == null)
        {
            error = "صيغة MSISDN غير صالحة — يجب أن يكون رقماً سورياً محلياً (09xxxxxxxx).";
            return false;
        }

        if (!SyrianLocalPattern.IsMatch(canonical))
        {
            error = "MSISDN لا يطابق القيود الهيكلية للشبكة.";
            return false;
        }

        normalized = canonical;
        return true;
    }

    [GeneratedRegex(@"^09\d{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex SyrianMsisdnRegex();
}
