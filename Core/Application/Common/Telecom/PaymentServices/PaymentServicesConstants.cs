namespace Application.Common.Telecom.PaymentServices;

public static class PaymentServicesConstants
{
    /// <summary>VAL-12-05 — نافذة عكس العملية (دقائق).</summary>
    public const int ReversalWindowMinutes = 120;

    /// <summary>VAL-12-04 — نافذة رصد تكرار الشحن.</summary>
    public const int FraudWindowMinutes = 2;

    public const int MaxRechargesPerFraudWindow = 3;

    public static readonly string[] RolesAllowedReversal =
    [
        "TelecomManagement",
        "TelecomBackOffice",
        "TelecomAdmin",
    ];
}
