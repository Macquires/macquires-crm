namespace Infrastructure.TelecomIntegrations;

using Application.Common.Telecom;

/// <summary>Deterministic CBS mock balances when HTTP mode is off — no per-MSISDN mutable state.</summary>
internal static class CbsMockBalanceResolver
{
    public static decimal ResolveOutstandingBalance(string? msisdn)
    {
        if (string.IsNullOrWhiteSpace(msisdn))
        {
            return 0m;
        }

        var normalized = msisdn.Trim();
        if (normalized == TelecomDemoMsisdn.DebtSubscriber)
        {
            return TelecomDemoBaselines.DebtOutstandingSyp;
        }

        var hash = Math.Abs(normalized.GetHashCode(StringComparison.Ordinal));

        if (normalized.EndsWith("9", StringComparison.Ordinal) || hash % 5 == 0)
        {
            return -(hash % 20_000 + 1_000m);
        }

        return hash % 15_000 + 500m;
    }
}
