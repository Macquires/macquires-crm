using Application.Common.Services.SecurityManager;
using Application.Common.Telecom;

namespace Application.Common.Dashboard;

public static class DashboardPersonaFilter
{
    public static bool WidgetVisibleForPersona(string personasAllowedCsv, string personaName) =>
        personasAllowedCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(personaName, StringComparer.OrdinalIgnoreCase);

    public static TelecomMenuPersona? ResolveEffectivePersona(
        IReadOnlyList<string> roles,
        string? previewPersona)
    {
        if (!string.IsNullOrWhiteSpace(previewPersona)
            && Enum.TryParse<TelecomMenuPersona>(previewPersona, true, out var parsed))
        {
            return parsed;
        }

        return TelecomPersonaResolver.ResolvePrimary(roles);
    }
}
