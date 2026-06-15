namespace Application.Common.Telecom.Analytics;

/// <summary>Approximate governorate coordinates for branch geo heatmap (Syria).</summary>
public static class SyriaBranchGeoCatalog
{
    private static readonly (string Keyword, double Lat, double Lng)[] CityHints =
    [
        ("دمشق", 33.5138, 36.2765),
        ("حلب", 36.2021, 37.1343),
        ("إدلب", 35.9306, 36.6339),
        ("اللاذقية", 35.5317, 35.7908),
        ("طرطوس", 34.8890, 35.8866),
        ("حمص", 34.7324, 36.7138),
        ("حماة", 35.1318, 36.7578),
        ("درعا", 32.6189, 36.1021),
        ("دير الزور", 35.3333, 40.1500),
        ("الحسكة", 36.5000, 40.7500),
        ("الرقة", 35.9500, 39.0167),
        ("السويداء", 32.7091, 36.5716),
    ];

    public static (double Lat, double Lng) Resolve(string? branchNameAr, string? branchNameEn)
    {
        var haystack = $"{branchNameAr} {branchNameEn}";
        foreach (var (keyword, lat, lng) in CityHints)
        {
            if (haystack.Contains(keyword, StringComparison.Ordinal))
            {
                return (lat, lng);
            }
        }

        return (34.8021, 38.9968);
    }
}
