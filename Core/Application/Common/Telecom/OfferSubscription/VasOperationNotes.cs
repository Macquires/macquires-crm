using System.Text.RegularExpressions;

namespace Application.Common.Telecom.OfferSubscription;

/// <summary>Parses VAS intent from operation notes (same format as toggle trail).</summary>
public static partial class VasOperationNotes
{
    [GeneratedRegex(@"^\s*(Activate|Deactivate)\s+VAS\s+([A-Z0-9_]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VasIntentPattern();

    public static bool TryParse(string? notes, out bool activate, out string serviceCode)
    {
        activate = false;
        serviceCode = string.Empty;
        if (string.IsNullOrWhiteSpace(notes))
        {
            return false;
        }

        var match = VasIntentPattern().Match(notes.Trim());
        if (!match.Success)
        {
            return false;
        }

        activate = match.Groups[1].Value.Equals("Activate", StringComparison.OrdinalIgnoreCase);
        serviceCode = match.Groups[2].Value.ToUpperInvariant();
        return true;
    }

    public static string Format(bool activate, string serviceCode) =>
        $"{(activate ? "Activate" : "Deactivate")} VAS {serviceCode.Trim().ToUpperInvariant()}";
}
