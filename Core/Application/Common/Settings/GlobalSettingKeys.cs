namespace Application.Common.Settings;



public static class GlobalSettingKeys

{

    public const string MaintenanceMode = "MaintenanceMode";

    public const string MaintenanceMessageAr = "MaintenanceMessageAr";

    public const string MaintenanceMessageEn = "MaintenanceMessageEn";

    public const string MaintenanceBypassIPs = "MaintenanceBypassIPs";

    public const string IsStrictPersonaMode = "IsStrictPersonaMode";

    /// <summary>وضعية العرض التجريبي — تُظهر مؤشر Mock Data للمخازن والتكامل مع Oracle Fusion.</summary>
    public const string IsDemoVersion = "System.IsDemoVersion";

    public const string JwtAccessTokenMinutes = "JwtAccessTokenMinutes";

    public const string IntegrationHuaweiEnabled = "Integration.Huawei.Enabled";

    public const string IntegrationHlrEnabled = "Integration.Hlr.Enabled";

    public const string IntegrationSmsEnabled = "Integration.Sms.Enabled";

    public const string IntegrationInEnabled = "Integration.IN.Enabled";

    public const string IntegrationInGatewayUrl = "Integration.IN.GatewayUrl";

    public const string IntegrationInTimeoutMilliseconds = "Integration.IN.TimeoutMilliseconds";

    public const string IntegrationInSimulateRealTimeDeduction = "Integration.IN.SimulateRealTimeDeduction";



    public const string NotificationSmsCustomerOpsEnabled = "Notification.Sms.CustomerOps.Enabled";

    public const string NotificationSmsWelcomeEnabled = "Notification.Sms.Welcome.Enabled";

    public const string NotificationSmsWelcomeTemplateAr = "Notification.Sms.WelcomeTemplateAr";

    public const string NotificationSmsWelcomeTemplateEn = "Notification.Sms.WelcomeTemplateEn";



    // Telecom — Inventory

    public const string TelecomInventoryMsisdnQuarantineDays = "Telecom.Inventory.MsisdnQuarantineDays";

    public const string TelecomInventorySimQuarantineDays = "Telecom.Inventory.SimQuarantineDays";

    public const string TelecomInventoryMsisdnReservationHours = "Telecom.Inventory.MsisdnReservationHours";

    public const string TelecomInventoryMsisdnReservationMinutes = "Telecom.Inventory.MsisdnReservationMinutes";

    public const string TelecomInventoryDormantLineScanIntervalHours = "Telecom.Inventory.DormantLineScanIntervalHours";

    public const string TelecomInventoryDormantLineInactivityDays = "Telecom.Inventory.DormantLineInactivityDays";

    public const string IntegrationOracleFusionEnabled = "Integration.OracleFusion.Enabled";

    public const string IntegrationOracleFusionSyncIntervalMinutes = "Integration.OracleFusion.SyncIntervalMinutes";



    // Telecom — Activation (ACT)

    /// <summary>Legacy key — kept for backward compatibility; synced with MaxLinesIndividual.</summary>

    public const string TelecomMaxActiveLinesPerIndividual = "Telecom.MaxActiveLinesPerIndividual";

    public const string TelecomActivationMaxLinesIndividual = "Telecom.Activation.MaxLinesIndividual";

    public const string TelecomActivationMaxLinesCorporate = "Telecom.Activation.MaxLinesCorporate";

    public const string TelecomActivationDealerCodeRequired = "Telecom.Activation.DealerCodeRequired";

    public const string TelecomActivationRequireKycBeforeConfirm = "Telecom.Activation.RequireKycBeforeConfirm";

    public const string TelecomActivationRequirePaymentReferenceWhenDepositDue = "Telecom.Activation.RequirePaymentReferenceWhenDepositDue";

    public const string TelecomActivationChannelShowroomLabelAr = "Telecom.Activation.Channel.Showroom.LabelAr";

    public const string TelecomActivationChannelShowroomLabelEn = "Telecom.Activation.Channel.Showroom.LabelEn";

    public const string TelecomActivationChannelDealerLabelAr = "Telecom.Activation.Channel.Dealer.LabelAr";

    public const string TelecomActivationChannelDealerLabelEn = "Telecom.Activation.Channel.Dealer.LabelEn";

    public const string TelecomActivationChannelDigitalLabelAr = "Telecom.Activation.Channel.Digital.LabelAr";

    public const string TelecomActivationChannelDigitalLabelEn = "Telecom.Activation.Channel.Digital.LabelEn";



    // Telecom — Termination (TRM)

    public const string TelecomTerminationDefaultFinalBillAmountSyp = "Telecom.Termination.DefaultFinalBillAmountSyp";

    public const string TelecomTerminationBackOfficeTypes = "Telecom.Termination.BackOfficeTypes";

    public const string TelecomTerminationCorporateRequiresBackOffice = "Telecom.Termination.CorporateRequiresBackOffice";

    public const string TelecomTerminationVoluntaryRequiresRetentionOutcome = "Telecom.Termination.VoluntaryRequiresRetentionOutcome";



    // Telecom — Refund (RFD)

    public const string TelecomRefundDualApprovalThresholdSyp = "Telecom.Refund.DualApprovalThresholdSyp";

    public const string TelecomRefundHighRiskMethods = "Telecom.Refund.HighRiskMethods";



    // Telecom — Suspension (SUS)

    public const string TelecomSuspensionLongReviewDays = "Telecom.Suspension.LongReviewDays";

    public const string TelecomSuspensionBackOfficeTypes = "Telecom.Suspension.BackOfficeTypes";

    public const string TelecomSuspensionCorporateRequiresBackOffice = "Telecom.Suspension.CorporateRequiresBackOffice";



    // Telecom — SIM Swap

    public const string TelecomSimSwapLostStolenRequiresBackOffice = "Telecom.SimSwap.LostStolenRequiresBackOffice";



    // Telecom — Change Number (CNR)

    public const string TelecomChangeNumberPremiumCategories = "Telecom.ChangeNumber.PremiumCategories";

    public const string TelecomChangeNumberBlockQuarantinedTarget = "Telecom.ChangeNumber.BlockQuarantinedTarget";



    // Telecom — Take Over (TKO)

    public const string TelecomTakeOverAlwaysRequiresBackOffice = "Telecom.TakeOver.AlwaysRequiresBackOffice";

    public const string TelecomTakeOverDefaultDepositTransferPolicy = "Telecom.TakeOver.DefaultDepositTransferPolicy";



    // Telecom — Device Sales

    public const string TelecomDeviceSalesCreditScoreLowThreshold = "Telecom.DeviceSales.CreditScoreLowThreshold";

    public const string TelecomDeviceSalesCreditScoreGoodThreshold = "Telecom.DeviceSales.CreditScoreGoodThreshold";

    public const string TelecomDeviceSalesDownPaymentPercentReject = "Telecom.DeviceSales.DownPaymentPercent.Reject";

    public const string TelecomDeviceSalesDownPaymentPercentVip = "Telecom.DeviceSales.DownPaymentPercent.Vip";

    public const string TelecomDeviceSalesDownPaymentPercentLow = "Telecom.DeviceSales.DownPaymentPercent.Low";

    public const string TelecomDeviceSalesDownPaymentPercentGood = "Telecom.DeviceSales.DownPaymentPercent.Good";

    // Telecom — Bad Debt Recovery (BDR)
    public const string TelecomBdrTicketSlaMinutes = "Telecom.Bdr.TicketSlaMinutes";
    public const string TelecomPostpaidBadDebtThresholdMonths = "Telecom.PostpaidBadDebtThresholdMonths";
}


