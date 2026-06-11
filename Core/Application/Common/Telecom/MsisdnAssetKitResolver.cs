using Application.Common.CQS.Queries;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom;

/// <summary>Resolves ICCID/IMSI for MSISDN pool rows (import pairing, profile SIM, demo kit derivation).</summary>
public static class MsisdnAssetKitResolver
{
    public static SimInventory? ResolveLinkedSim(IEnumerable<SimInventory>? sims)
    {
        if (sims == null)
        {
            return null;
        }

        return sims
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.Status == SimStatus.Active)
            .ThenByDescending(s => s.Status == SimStatus.Reserved)
            .ThenByDescending(s => s.CreatedAtUtc)
            .FirstOrDefault();
    }

    public static string SimTypeCode(SimType? simType) => simType switch
    {
        SimType.ESim => "ESim",
        SimType.Physical => "Physical",
        _ => "Physical",
    };

    public static string SimTypeLabel(SimType? simType) => SimTypeCode(simType);

    public static string SimStatusLabel(SimStatus? status) => status switch
    {
        SimStatus.Available => "متاحة",
        SimStatus.Reserved => "محجوزة",
        SimStatus.Active => "نشطة",
        SimStatus.Suspended => "موقوفة",
        SimStatus.Quarantined => "حجر",
        _ => "—",
    };

    public static SimStatus? DefaultSimStatusForProfile(SubscriberOperationalStatus? profileStatus) =>
        profileStatus == SubscriberOperationalStatus.Active ? SimStatus.Active : null;

    public static bool TryParseSyrianMsisdnSerial(string? msisdn, out long serial)
    {
        serial = 0;
        if (string.IsNullOrWhiteSpace(msisdn)) return false;
        var digits = new string(msisdn.Where(char.IsDigit).ToArray());
        if (digits.Length < 10 || !digits.StartsWith("09", StringComparison.Ordinal)) return false;
        return long.TryParse(digits[2..], out serial) && serial > 0;
    }

    public static string? DeriveIccidFromMsisdn(string? msisdn)
    {
        if (!TryParseSyrianMsisdnSerial(msisdn, out var serial)) return null;
        return BuildIccidFromImportSerial(serial);
    }

    public static string? DeriveImsiFromMsisdn(string? msisdn)
    {
        if (!TryParseSyrianMsisdnSerial(msisdn, out var serial)) return null;
        return $"41701{serial % 10_000_000_000L:D10}";
    }

    public static string BuildIccidFromImportSerial(long serial)
    {
        var base19 = $"891030{serial % 1_000_000_000_000L:D12}";
        return base19 + GetLuhnCheckDigit(base19);
    }

    public static string GetLuhnCheckDigit(string digitsWithoutCheck)
    {
        var sum = 0;
        var alt = true;
        for (var i = digitsWithoutCheck.Length - 1; i >= 0; i--)
        {
            var n = digitsWithoutCheck[i] - '0';
            if (alt)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alt = !alt;
        }

        return ((10 - (sum % 10)) % 10).ToString();
    }

    /// <summary>Pool dashboard — pairing is shown only after a subscriber transaction binds MSISDN and SIM.</summary>
    public static bool ShouldExposePoolPairing(MsisdnPoolStatus status) =>
        status is MsisdnPoolStatus.Active or MsisdnPoolStatus.Reserved or MsisdnPoolStatus.Suspended;

    public static (string? Iccid, string? Imsi) ResolveForPoolDisplay(
        MsisdnAsset asset,
        SimInventory? profileSim,
        SimInventory? operationSim,
        IReadOnlyDictionary<string, SimInventory> simsByIccid)
    {
        if (!ShouldExposePoolPairing(asset.PoolStatus))
        {
            return (null, null);
        }

        return Resolve(asset, profileSim, operationSim, simsByIccid);
    }

    public static (string? Iccid, string? Imsi) Resolve(
        MsisdnAsset asset,
        SimInventory? profileSim,
        SimInventory? operationSim,
        IReadOnlyDictionary<string, SimInventory> simsByIccid)
    {
        SimInventory? sim = operationSim ?? profileSim;

        if (sim == null && !string.IsNullOrWhiteSpace(asset.PairedIccid)
            && simsByIccid.TryGetValue(asset.PairedIccid.Trim(), out var pairedSim))
        {
            sim = pairedSim;
        }

        var derivedIccid = DeriveIccidFromMsisdn(asset.Msisdn);
        if (sim == null && !string.IsNullOrWhiteSpace(derivedIccid)
            && simsByIccid.TryGetValue(derivedIccid, out var derivedSim))
        {
            sim = derivedSim;
        }

        var iccid = sim?.Iccid ?? asset.PairedIccid ?? derivedIccid;
        var imsi = sim?.Imsi ?? asset.PairedImsi ?? DeriveImsiFromMsisdn(asset.Msisdn);
        return (iccid, imsi);
    }

    public static HashSet<string> CollectIccidCandidates(MsisdnAsset asset)
    {
        var candidates = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(asset.PairedIccid))
        {
            candidates.Add(asset.PairedIccid.Trim());
        }

        var derivedIccid = DeriveIccidFromMsisdn(asset.Msisdn);
        if (!string.IsNullOrWhiteSpace(derivedIccid))
        {
            candidates.Add(derivedIccid);
        }

        return candidates;
    }

    public static async Task<Dictionary<string, SimInventory>> LoadSimsByIccidAsync(
        IQueryContext query,
        IEnumerable<string> iccidCandidates,
        CancellationToken cancellationToken = default)
    {
        var candidates = iccidCandidates
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (candidates.Count == 0)
        {
            return new Dictionary<string, SimInventory>(StringComparer.Ordinal);
        }

        return await query.SimInventory.AsNoTracking()
            .Where(s => !s.IsDeleted && candidates.Contains(s.Iccid))
            .ToDictionaryAsync(s => s.Iccid, StringComparer.Ordinal, cancellationToken);
    }

    public static async Task<(string? Iccid, string? Imsi)> ResolveForAssetAsync(
        IQueryContext query,
        MsisdnAsset asset,
        string? subscriberProfileId,
        string? operationSimInventoryId = null,
        CancellationToken cancellationToken = default)
    {
        SimInventory? operationSim = null;
        if (!string.IsNullOrWhiteSpace(operationSimInventoryId))
        {
            operationSim = await query.SimInventory.AsNoTracking()
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == operationSimInventoryId, cancellationToken);
        }

        SimInventory? profileSim = null;
        if (!string.IsNullOrWhiteSpace(subscriberProfileId))
        {
            var profileSims = await query.SimInventory.AsNoTracking()
                .Where(s => !s.IsDeleted && s.SubscriberProfileId == subscriberProfileId)
                .ToListAsync(cancellationToken);
            profileSim = ResolveLinkedSim(profileSims);
        }

        var simsByIccid = await LoadSimsByIccidAsync(query, CollectIccidCandidates(asset), cancellationToken);
        return Resolve(asset, profileSim, operationSim, simsByIccid);
    }
}
