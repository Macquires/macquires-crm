using Application.Common.Telecom;

namespace Application.Common.Integrations;

/// <summary>
/// Single source of truth for Oracle Fusion SCM mock inventory — used by mock client, sync, and DB seed.
/// MSISDN and SIM feeds are decoupled (zero warehouse-level FK).
/// </summary>
public static class OracleFusionInventoryCatalog
{
    public static OracleInventoryPullResult BuildStandardPull() =>
        new(
            true,
            "Oracle Fusion SCM standard demo inventory batch.",
            BuildMsisdnFeed(),
            BuildSimFeed());

    private static IReadOnlyList<OracleMsisdnFeedItem> BuildMsisdnFeed()
    {
        var items = new List<OracleMsisdnFeedItem>();

        AddMsisdn(items, TelecomDemoMsisdn.ShowcaseHealthy, "963", "93", "Normal");
        AddMsisdn(items, TelecomDemoMsisdn.Hero, "963", "93", "Normal");
        AddMsisdn(items, TelecomDemoMsisdn.DebtSubscriber, "963", "93", "Normal");
        AddMsisdn(items, TelecomDemoMsisdn.ShowcaseNotProvisioned, "963", "93", "Normal");
        AddMsisdn(items, TelecomDemoMsisdn.ShowcaseOperationalSuspended, "963", "93", "Normal");
        AddMsisdn(items, TelecomDemoMsisdn.ReconnectFraudDemo, "963", "97", "Silver");

        for (var i = 1; i <= 100; i++)
        {
            var prefix = i % 2 == 0 ? "093" : "099";
            var num = $"{prefix}{i + 2_000_000:0000000}";
            AddMsisdn(items, num, "963", prefix[1..], i % 5 == 0 ? "Silver" : "Normal");
        }

        for (var i = 1; i <= 3; i++)
        {
            AddMsisdn(items, $"093555000{i}", "963", "93", "Normal");
        }

        for (var i = 1; i <= 5; i++)
        {
            AddMsisdn(items, $"093999000{i}", "963", "93", "Normal");
        }

        AddMsisdn(items, "0933012345", "963", "93", "Normal");
        AddMsisdn(items, "0977012345", "963", "97", "Silver");

        return items;
    }

    private static IReadOnlyList<OracleSimFeedItem> BuildSimFeed()
    {
        var sims = new List<OracleSimFeedItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var msisdn in BuildMsisdnFeed().Select(m => m.Msisdn))
        {
            var iccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(msisdn);
            if (string.IsNullOrWhiteSpace(iccid) || !seen.Add(iccid))
            {
                continue;
            }

            var imsi = MsisdnAssetKitResolver.DeriveImsiFromMsisdn(msisdn);
            sims.Add(new OracleSimFeedItem(iccid, imsi, null, null));
        }

        return sims;
    }

    private static void AddMsisdn(
        ICollection<OracleMsisdnFeedItem> items,
        string msisdn,
        string countryCode,
        string prefix,
        string category)
    {
        if (!MsisdnValidator.TryValidate(msisdn, out var normalized, out _))
        {
            return;
        }

        items.Add(new OracleMsisdnFeedItem(normalized, countryCode, prefix, category));
    }
}
