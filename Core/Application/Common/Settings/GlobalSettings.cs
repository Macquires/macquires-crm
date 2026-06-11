namespace Application.Common.Settings;

/// <summary>
/// Static wrapper for dynamic global configuration parameters.
/// Updated at runtime via <see cref="IGlobalSettingsProvider"/>.
/// </summary>
public static class GlobalSettings
{
    /// <summary>
    /// Dictates how many months an invoice can remain unpaid before it is legally classified as "Bad Debt".
    /// Default: 6 months.
    /// </summary>
    public static int PostpaidBadDebtThresholdMonths { get; set; } = 6;
}
