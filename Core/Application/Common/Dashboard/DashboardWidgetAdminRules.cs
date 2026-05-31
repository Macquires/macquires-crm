using Application.Common.Services.SecurityManager;
using Domain.Enums;

namespace Application.Common.Dashboard;

public static class DashboardWidgetAdminRules
{
    public static string NormalizeWidgetKey(string? key) =>
        (key ?? string.Empty).Trim().ToLowerInvariant();

    public static string NormalizePersonasCsv(IEnumerable<string>? personas)
    {
        if (personas == null)
        {
            return string.Empty;
        }

        var valid = Enum.GetNames<TelecomMenuPersona>();
        var parts = personas
            .Select(p => p?.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(p => valid.Contains(p!, StringComparer.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return string.Join(',', parts);
    }

    public static void ValidatePersonasCsv(string? personasCsv)
    {
        if (string.IsNullOrWhiteSpace(personasCsv))
        {
            throw new InvalidOperationException("يجب تحديد دور واحد على الأقل (Persona).");
        }

        var valid = Enum.GetNames<TelecomMenuPersona>();
        foreach (var part in personasCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!valid.Contains(part, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"دور غير صالح: {part}");
            }
        }
    }

    public static void ValidateProviderKey(
        IDashboardWidgetRegistry registry,
        string? providerKey,
        DashboardWidgetKind kind)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
        {
            if (kind is DashboardWidgetKind.Stat or DashboardWidgetKind.StatusList)
            {
                throw new InvalidOperationException("مفتاح المزوّد (Provider) مطلوب لهذا النوع من الكرت.");
            }

            return;
        }

        if (!registry.IsRegistered(providerKey.Trim()))
        {
            throw new InvalidOperationException($"مزوّد البيانات غير مسجّل: {providerKey}");
        }
    }
}
