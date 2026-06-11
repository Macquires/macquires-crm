using System.Globalization;

namespace Application.Common;

/// <summary>User-facing copy in Arabic and English.</summary>
public sealed record BilingualUserMessage(string Ar, string En, string? Code = null)
{
    public string Resolve(bool preferArabic) => preferArabic ? Ar : En;

    public string ResolveForCurrentCulture()
    {
        var twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return Resolve(string.Equals(twoLetter, "ar", StringComparison.OrdinalIgnoreCase));
    }
}
