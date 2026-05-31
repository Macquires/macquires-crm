namespace Application.Common.Telecom;

/// <summary>ICCID validation (19–20 digits + Luhn check digit).</summary>
public static class IccidValidator
{
    public const int StandardLength = 20;

    public static bool TryValidate(string? iccid, out string normalized, out string? error)
    {
        normalized = "";
        error = null;
        if (string.IsNullOrWhiteSpace(iccid))
        {
            error = "ICCID فارغ.";
            return false;
        }

        normalized = new string(iccid.Where(char.IsDigit).ToArray());
        if (normalized.Length is < 19 or > 20)
        {
            error = "ICCID يجب أن يتكون من 19 أو 20 رقماً.";
            return false;
        }

        if (normalized.Length == 20 && !PassesLuhn(normalized))
        {
            error = "ICCID غير صالح (فشل تحقق Luhn).";
            return false;
        }

        return true;
    }

    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var alt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var n = digits[i] - '0';
            if (alt)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alt = !alt;
        }
        return sum % 10 == 0;
    }
}
