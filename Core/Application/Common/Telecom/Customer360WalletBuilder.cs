using Application.Features.CustomerManager.Queries;
using Domain.Enums;

namespace Application.Common.Telecom;

/// <summary>Builds per-line wallet snapshots (balance + package usage) for Customer 360.</summary>
public static class Customer360WalletBuilder
{
    public static string? NormalizeMsisdn(string? raw)
    {
        var m = (raw ?? "").Trim();
        if (m.Length == 10 && m.StartsWith("09", StringComparison.Ordinal))
            return "093" + m[2..];
        return string.IsNullOrEmpty(m) ? null : m;
    }

    public static decimal SimulateBalance(string msisdn)
    {
        var hash = Math.Abs(msisdn.GetHashCode(StringComparison.Ordinal));
        return hash % 45001 + 5000;
    }

    public static Customer360LineWalletDto Build(
        string? msisdn,
        decimal? crmPrepaidBalance,
        IReadOnlyList<Customer360PackageComponentDto> packageComponents,
        bool simulateDemoUsage = true)
    {
        var normalized = NormalizeMsisdn(msisdn);
        if (string.IsNullOrEmpty(normalized))
        {
            return new Customer360LineWalletDto(
                false,
                msisdn,
                crmPrepaidBalance,
                "SYP",
                "noMsisdnForWallet",
                []);
        }

        var balance = crmPrepaidBalance
            ?? (simulateDemoUsage ? SimulateBalance(normalized) : 0m);
        var components = packageComponents.Count > 0
            ? packageComponents
            : DefaultPackageComponents(normalized);

        var buckets = components
            .Select(c => ToUsageBucket(c, normalized, simulateDemoUsage))
            .ToList();

        return new Customer360LineWalletDto(true, msisdn, balance, "SYP", null, buckets);
    }

    /// <summary>
    /// Demo wallet uses hash-based partial quota usage; operator-created lines show full quotas and zero balance until CBS/IN sync.
    /// </summary>
    public static bool ShouldSimulateDemoWallet(string? customerCreatedById) =>
        string.IsNullOrWhiteSpace(customerCreatedById)
        || string.Equals(customerCreatedById, "system-seed", StringComparison.OrdinalIgnoreCase);

    private static List<Customer360PackageComponentDto> DefaultPackageComponents(string msisdn)
    {
        var seed = Math.Abs(msisdn.GetHashCode(StringComparison.Ordinal));
        return
        [
            new(ServiceComponentType.Data, null, 10 + seed % 15, "GB", false, 0),
            new(ServiceComponentType.Voice, null, 200 + seed % 400, "minutes", false, 1),
            new(ServiceComponentType.Sms, null, 50 + seed % 150, "SMS", false, 2),
        ];
    }

    private static Customer360UsageBucketDto ToUsageBucket(
        Customer360PackageComponentDto component,
        string msisdn,
        bool simulateDemoUsage)
    {
        if (component.IsUnlimited)
        {
            return new Customer360UsageBucketDto(
                component.ComponentType,
                component.Label,
                null,
                null,
                component.QuotaUnit,
                true,
                100);
        }

        var included = component.Quota ?? 0;
        if (included <= 0)
        {
            return new Customer360UsageBucketDto(
                component.ComponentType,
                component.Label,
                0,
                0,
                component.QuotaUnit,
                false,
                0);
        }

        if (!simulateDemoUsage)
        {
            return new Customer360UsageBucketDto(
                component.ComponentType,
                component.Label,
                included,
                included,
                component.QuotaUnit,
                false,
                0);
        }

        var seed = $"{msisdn}|{component.ComponentType}|{component.Label}".GetHashCode(StringComparison.Ordinal);
        var remainingPct = Math.Abs(seed) % 86 + 10;
        var remaining = Math.Round(included * remainingPct / 100m, 2);
        var used = Math.Max(0, included - remaining);
        var usagePercent = included > 0 ? (int)Math.Round(used / included * 100) : 0;

        return new Customer360UsageBucketDto(
            component.ComponentType,
            component.Label,
            included,
            remaining,
            component.QuotaUnit,
            false,
            Math.Min(100, usagePercent));
    }
}
