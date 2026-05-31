using Domain.Entities;
using Domain.Enums;

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

    public static string SimTypeLabel(SimType? simType) => simType switch
    {
        SimType.ESim => "eSIM",
        SimType.Physical => "فيزيائية",
        _ => "فيزيائية",
    };

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
}
