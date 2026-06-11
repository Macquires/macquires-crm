namespace Application.Common.Settings.Telecom;

/// <summary>مصدر الحقيقة الوحيد لقيم الافتراضية ومفاتيح قواعد أعمال Telecom.</summary>
public static class TelecomBusinessRulesCatalog
{
    public const string CategoryInventory = "Telecom.Inventory";
    public const string CategoryActivation = "Telecom.Activation";
    public const string CategoryTermination = "Telecom.Termination";
    public const string CategoryRefund = "Telecom.Refund";
    public const string CategorySuspension = "Telecom.Suspension";
    public const string CategorySimSwap = "Telecom.SimSwap";
    public const string CategoryChangeNumber = "Telecom.ChangeNumber";
    public const string CategoryTakeOver = "Telecom.TakeOver";
    public const string CategoryDeviceSales = "Telecom.DeviceSales";
    public const string CategoryBadDebt = "Telecom.BadDebt";

    public static IReadOnlyList<SettingDefinition> All { get; } = BuildAll();

    private static IReadOnlyList<SettingDefinition> BuildAll() =>
    [
        // Inventory & lifecycle
        Def(GlobalSettingKeys.TelecomInventoryMsisdnQuarantineDays, CategoryInventory, SettingValueType.Int, "90", 1, 365),
        Def(GlobalSettingKeys.TelecomInventorySimQuarantineDays, CategoryInventory, SettingValueType.Int, "90", 1, 365),
        Def(GlobalSettingKeys.TelecomInventoryMsisdnReservationMinutes, CategoryInventory, SettingValueType.Int, "15", 5, 1440,
            labelAr: "قفل الرقم أثناء البيع (دقائق)", labelEn: "POS MSISDN lock (minutes)"),
        Def(GlobalSettingKeys.TelecomInventoryMsisdnReservationHours, CategoryInventory, SettingValueType.Int, "24", 1, 168,
            labelAr: "احتياطي — حجز الرقم (ساعات)", labelEn: "Fallback MSISDN reservation (hours)"),
        Def(GlobalSettingKeys.TelecomInventoryDormantLineScanIntervalHours, CategoryInventory, SettingValueType.Int, "6", 1, 168,
            labelAr: "فترة فحص الخطوط الميتة (ساعات)", labelEn: "Dormant line scan interval (hours)"),
        Def(GlobalSettingKeys.TelecomInventoryDormantLineInactivityDays, CategoryInventory, SettingValueType.Int, "365", 30, 730,
            labelAr: "عتبة خمول الخط المدفوع مسبقاً (أيام)", labelEn: "Prepaid dormancy threshold (days)"),
        Def(GlobalSettingKeys.TelecomSuspensionLongReviewDays, CategorySuspension, SettingValueType.Int, "90", 1, 365),

        // ACT
        Def(GlobalSettingKeys.TelecomActivationMaxLinesIndividual, CategoryActivation, SettingValueType.Int, "5", 1, 20),
        Def(GlobalSettingKeys.TelecomActivationMaxLinesCorporate, CategoryActivation, SettingValueType.Int, "50", 1, 200),
        Def(GlobalSettingKeys.TelecomActivationDealerCodeRequired, CategoryActivation, SettingValueType.Bool, "true"),
        Def(GlobalSettingKeys.TelecomActivationRequireKycBeforeConfirm, CategoryActivation, SettingValueType.Bool, "true"),
        Def(GlobalSettingKeys.TelecomActivationRequirePaymentReferenceWhenDepositDue, CategoryActivation, SettingValueType.Bool, "true"),
        Def(GlobalSettingKeys.TelecomActivationChannelShowroomLabelAr, CategoryActivation, SettingValueType.String, "نقطة البيع", labelAr: "تسمية قناة POS — عربي", labelEn: "POS channel label — Arabic"),
        Def(GlobalSettingKeys.TelecomActivationChannelShowroomLabelEn, CategoryActivation, SettingValueType.String, "POS", labelAr: "تسمية قناة POS — إنجليزي", labelEn: "POS channel label — English"),
        Def(GlobalSettingKeys.TelecomActivationChannelDealerLabelAr, CategoryActivation, SettingValueType.String, "موزع", labelAr: "تسمية قناة الموزع — عربي", labelEn: "Dealer channel label — Arabic"),
        Def(GlobalSettingKeys.TelecomActivationChannelDealerLabelEn, CategoryActivation, SettingValueType.String, "Dealer", labelAr: "تسمية قناة الموزع — إنجليزي", labelEn: "Dealer channel label — English"),
        Def(GlobalSettingKeys.TelecomActivationChannelDigitalLabelAr, CategoryActivation, SettingValueType.String, "رقمي", labelAr: "تسمية القناة الرقمية — عربي", labelEn: "Digital channel label — Arabic"),
        Def(GlobalSettingKeys.TelecomActivationChannelDigitalLabelEn, CategoryActivation, SettingValueType.String, "Digital", labelAr: "تسمية القناة الرقمية — إنجليزي", labelEn: "Digital channel label — English"),

        // TRM
        Def(GlobalSettingKeys.TelecomTerminationDefaultFinalBillAmountSyp, CategoryTermination, SettingValueType.Decimal, "12500", 0, 10_000_000),
        Def(GlobalSettingKeys.TelecomTerminationBackOfficeTypes, CategoryTermination, SettingValueType.Csv, "Fraud,Regulatory,Collections"),
        Def(GlobalSettingKeys.TelecomTerminationCorporateRequiresBackOffice, CategoryTermination, SettingValueType.Bool, "true"),
        Def(GlobalSettingKeys.TelecomTerminationVoluntaryRequiresRetentionOutcome, CategoryTermination, SettingValueType.Bool, "true"),

        // RFD
        Def(GlobalSettingKeys.TelecomRefundDualApprovalThresholdSyp, CategoryRefund, SettingValueType.Decimal, "500000", 0, 100_000_000),
        Def(GlobalSettingKeys.TelecomRefundHighRiskMethods, CategoryRefund, SettingValueType.Csv, "SyriatelCash"),

        // SUS
        Def(GlobalSettingKeys.TelecomSuspensionBackOfficeTypes, CategorySuspension, SettingValueType.Csv, "Fraud,Regulatory"),
        Def(GlobalSettingKeys.TelecomSuspensionCorporateRequiresBackOffice, CategorySuspension, SettingValueType.Bool, "true"),

        // SIM
        Def(GlobalSettingKeys.TelecomSimSwapLostStolenRequiresBackOffice, CategorySimSwap, SettingValueType.Bool, "true"),

        // CNR
        Def(GlobalSettingKeys.TelecomChangeNumberPremiumCategories, CategoryChangeNumber, SettingValueType.Csv, "Silver,Gold,Platinum"),
        Def(GlobalSettingKeys.TelecomChangeNumberBlockQuarantinedTarget, CategoryChangeNumber, SettingValueType.Bool, "true"),

        // TKO
        Def(GlobalSettingKeys.TelecomTakeOverAlwaysRequiresBackOffice, CategoryTakeOver, SettingValueType.Bool, "true"),
        Def(GlobalSettingKeys.TelecomTakeOverDefaultDepositTransferPolicy, CategoryTakeOver, SettingValueType.String, "TransferToNewOwner"),

        // Device sales
        Def(GlobalSettingKeys.TelecomDeviceSalesCreditScoreLowThreshold, CategoryDeviceSales, SettingValueType.Int, "550", 300, 900),
        Def(GlobalSettingKeys.TelecomDeviceSalesCreditScoreGoodThreshold, CategoryDeviceSales, SettingValueType.Int, "650", 300, 900),
        Def(GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentReject, CategoryDeviceSales, SettingValueType.Decimal, "100", 0, 100),
        Def(GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentVip, CategoryDeviceSales, SettingValueType.Decimal, "10", 0, 100),
        Def(GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentLow, CategoryDeviceSales, SettingValueType.Decimal, "20", 0, 100),
        Def(GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentGood, CategoryDeviceSales, SettingValueType.Decimal, "15", 0, 100),

        // BDR
        Def(GlobalSettingKeys.TelecomBdrTicketSlaMinutes, CategoryBadDebt, SettingValueType.Int, "2", 1, 1440,
            labelAr: "مهلة معالجة طلبات تحصيل الديون (دقائق)", labelEn: "BDR Ticket SLA (minutes)"),
        Def(GlobalSettingKeys.TelecomPostpaidBadDebtThresholdMonths, CategoryBadDebt, SettingValueType.Int, "6", 1, 120,
            labelAr: "عتبة الديون المتعثرة (أشهر)", labelEn: "Postpaid Bad Debt Threshold (months)"),
    ];

    public static SettingDefinition? FindByKey(string key) =>
        All.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));

    private static SettingDefinition Def(
        string key,
        string category,
        SettingValueType type,
        string defaultValue,
        decimal? min = null,
        decimal? max = null,
        string? labelAr = null,
        string? labelEn = null) =>
        new(key, category, type, defaultValue, min, max, labelAr, labelEn);
}
