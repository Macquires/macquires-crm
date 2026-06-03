using Domain.Common;

namespace Application.Common.Telecom.ChangeGsm;

/// <summary>VAL-06-01 — allowed Prepaid / Postpaid / Hybrid transitions.</summary>
public static class ChangeGsmTransitionMatrix
{
    private static readonly Dictionary<string, HashSet<string>> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [TelecomSubscriptionTypeWellKnownIds.Prepaid] = new(StringComparer.OrdinalIgnoreCase)
        {
            TelecomSubscriptionTypeWellKnownIds.Postpaid,
            TelecomSubscriptionTypeWellKnownIds.Hybrid,
        },
        [TelecomSubscriptionTypeWellKnownIds.Postpaid] = new(StringComparer.OrdinalIgnoreCase)
        {
            TelecomSubscriptionTypeWellKnownIds.Prepaid,
            TelecomSubscriptionTypeWellKnownIds.Hybrid,
        },
        [TelecomSubscriptionTypeWellKnownIds.Hybrid] = new(StringComparer.OrdinalIgnoreCase)
        {
            TelecomSubscriptionTypeWellKnownIds.Prepaid,
            TelecomSubscriptionTypeWellKnownIds.Postpaid,
        },
    };

    public static bool IsAllowed(string sourceTypeId, string targetTypeId) =>
        Allowed.TryGetValue(sourceTypeId, out var targets) && targets.Contains(targetTypeId);

    public static IReadOnlyList<string> GetAllowedTargets(string sourceTypeId) =>
        Allowed.TryGetValue(sourceTypeId, out var targets)
            ? targets.ToList()
            : Array.Empty<string>();
}
