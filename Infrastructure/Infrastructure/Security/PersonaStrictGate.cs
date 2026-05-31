using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Application.Common.Settings;
using Application.Common.Telecom;
using Infrastructure.SecurityManager.NavigationMenu;
using System.Text.Json;

namespace Infrastructure.Security;

public class PersonaStrictGate : IPersonaStrictGate
{
    private readonly IGlobalSettingsProvider _settings;
    private static readonly Lazy<IReadOnlyDictionary<TelecomMenuPersona, HashSet<string>>> PathIndex =
        new(BuildPathIndex);

    public PersonaStrictGate(IGlobalSettingsProvider settings) => _settings = settings;

    public Task<bool> IsStrictModeEnabledAsync(CancellationToken cancellationToken = default) =>
        _settings.GetBoolAsync(GlobalSettingKeys.IsStrictPersonaMode, defaultValue: true, cancellationToken);

    public bool IsPersonaAllowedForPath(TelecomMenuPersona persona, string requestPath)
    {
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return true;
        }

        var normalized = NormalizePath(requestPath);
        if (normalized.Length == 0)
        {
            return true;
        }

        if (!PathIndex.Value.TryGetValue(persona, out var allowed))
        {
            return false;
        }

        return allowed.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsPersonaAllowedForCommand(TelecomMenuPersona persona, Type requestType)
    {
        var allowed = PersonaCommandAccessRules.GetAllowedPersonas(requestType);
        if (allowed == null || allowed.Count == 0)
        {
            return true;
        }

        return allowed.Contains(persona);
    }

    private static string NormalizePath(string path)
    {
        var p = path.Split('?')[0].TrimEnd('/');
        if (p.Length == 0)
        {
            return "/";
        }

        return p.StartsWith('/') ? p : "/" + p;
    }

    private static IReadOnlyDictionary<TelecomMenuPersona, HashSet<string>> BuildPathIndex()
    {
        var json = NavigationTreeStructure.JsonStructure;
        var items = JsonSerializer.Deserialize<List<NavItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];

        var map = new Dictionary<TelecomMenuPersona, HashSet<string>>();

        void Walk(List<NavItem>? nodes)
        {
            if (nodes == null)
            {
                return;
            }

            foreach (var node in nodes)
            {
                if (!string.IsNullOrWhiteSpace(node.URL) && node.URL != "#" && node.Personas is { Count: > 0 })
                {
                    var path = NormalizePath(node.URL);
                    foreach (var pName in node.Personas)
                    {
                        if (!Enum.TryParse<TelecomMenuPersona>(pName, true, out var persona))
                        {
                            continue;
                        }

                        if (!map.TryGetValue(persona, out var set))
                        {
                            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            map[persona] = set;
                        }

                        set.Add(path);
                    }
                }

                Walk(node.Children);
            }
        }

        Walk(items);

        // Customer lookup pages (not sidebar items) — same personas as subscriber registry search.
        var lookupPaths = new[] { "/Telecom/UnifiedSearch", "/Telecom/Customer360Profile" };
        var lookupPersonas = new[]
        {
            TelecomMenuPersona.Executive,
            TelecomMenuPersona.CallCenter,
            TelecomMenuPersona.Retail,
            TelecomMenuPersona.BackOffice,
            TelecomMenuPersona.SysAdmin,
        };
        foreach (var persona in lookupPersonas)
        {
            if (!map.TryGetValue(persona, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[persona] = set;
            }

            foreach (var path in lookupPaths)
            {
                set.Add(path);
            }
        }

        foreach (var persona in Enum.GetValues<TelecomMenuPersona>())
        {
            if (!map.ContainsKey(persona))
            {
                map[persona] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            map[persona].Add("/Accounts");
            map[persona].Add("/Dashboards/DefaultDashboard");
            map[persona].Add(TelecomPersonaLanding.TelecomHub);
        }

        return map;
    }

    private sealed class NavItem
    {
        public string? URL { get; set; }
        public List<string>? Personas { get; set; }
        public List<NavItem>? Children { get; set; }
    }
}
