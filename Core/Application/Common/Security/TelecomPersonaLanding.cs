using Application.Common.Services.SecurityManager;

namespace Application.Common.Security;

/// <summary>Post-login landing routes per operator persona (Syriatel telecom workspace).</summary>
public static class TelecomPersonaLanding
{
    public const string TelecomHub = "/Telecom/TelecomHub";
    public const string ExecutiveCommandCenter = "/Executive/CommandCenter?tab=scorecard";
    public const string SysAdmin = TelecomHub;
    public const string BackOffice = "/Telecom/BackOfficeDashboard";
    public const string Executive = ExecutiveCommandCenter;
    public const string Retail = TelecomHub;
    public const string CallCenter = TelecomHub;
    public const string DefaultFallback = TelecomHub;

    public static string ResolvePath(TelecomMenuPersona? persona) =>
        persona switch
        {
            TelecomMenuPersona.SysAdmin => SysAdmin,
            TelecomMenuPersona.BackOffice => BackOffice,
            TelecomMenuPersona.Executive => Executive,
            TelecomMenuPersona.Retail => Retail,
            TelecomMenuPersona.CallCenter => CallCenter,
            _ => DefaultFallback,
        };

    public static string ResolvePath(string? personaName)
    {
        if (string.IsNullOrWhiteSpace(personaName))
        {
            return DefaultFallback;
        }

        return Enum.TryParse<TelecomMenuPersona>(personaName, true, out var persona)
            ? ResolvePath(persona)
            : DefaultFallback;
    }
}
