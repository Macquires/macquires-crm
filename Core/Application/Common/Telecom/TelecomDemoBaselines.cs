namespace Application.Common.Telecom;

/// <summary>
/// Canonical demo telemetry for national showcase anchors — CBS, CRM wallet, and wizard refs stay aligned.
/// </summary>
public static class TelecomDemoBaselines
{
    /// <summary>مازن المديون — overdue postpaid ledger (CBS + CRM).</summary>
    public const decimal DebtOutstandingSyp = -15_000m;

    /// <summary>Full cash payment at showroom (wizard 3) — matches |<see cref="DebtOutstandingSyp"/>|.</summary>
    public const decimal DebtFullPaymentAmountSyp = 15_000m;

    /// <summary>Postpaid credit limit restored after full debt settlement (مازن showcase).</summary>
    public const decimal DebtRestoredPostpaidLimitSyp = 200_000m;

    /// <summary>Full cash payment at showroom (wizard 3) — never pre-seeded on CBS.</summary>
    public const string DebtFullPaymentReceipt = "RCPT-2002";

    public static bool IsDebtFullPaymentReceipt(string? reference) =>
        string.Equals(reference?.Trim(), DebtFullPaymentReceipt, StringComparison.OrdinalIgnoreCase);

    /// <summary>Commercial postpaid offering for the debt showcase line (not prepaid Ya Hala).</summary>
    public const string DebtPostpaidOfferingCode = "POST_CLASSIC";

    /// <summary>Billing suspension age — exceeds 6-month BDR / bad-debt threshold.</summary>
    public const int DebtBillingSuspensionMonthsAgo = 7;

    /// <summary>Subscriber tenure before billing suspension (realistic invoice customer).</summary>
    public const int DebtSubscriberTenureMonthsBeforeSuspension = 18;

    public static DateTime DebtBillingSuspensionStartUtc(DateTime? referenceUtc = null)
    {
        var anchor = referenceUtc ?? DateTime.UtcNow;
        return anchor.AddMonths(-DebtBillingSuspensionMonthsAgo);
    }

    public static DateTime DebtLineActivatedUtc(DateTime? referenceUtc = null)
    {
        var anchor = referenceUtc ?? DateTime.UtcNow;
        return anchor.AddMonths(-(DebtBillingSuspensionMonthsAgo + DebtSubscriberTenureMonthsBeforeSuspension));
    }

    public static bool IsDebtShowcaseMsisdn(string? msisdn) =>
        string.Equals(
            TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(msisdn ?? string.Empty),
            TelecomDemoMsisdn.DebtSubscriber,
            StringComparison.Ordinal);
}
