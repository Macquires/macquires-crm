namespace Application.Common.Settings.Telecom;

public class TelecomBusinessRulesDto
{
    public TelecomInventoryRulesDto Inventory { get; set; } = new();
    public TelecomActivationRulesDto Activation { get; set; } = new();
    public TelecomTerminationRulesDto Termination { get; set; } = new();
    public TelecomRefundRulesDto Refund { get; set; } = new();
    public TelecomSuspensionRulesDto Suspension { get; set; } = new();
    public TelecomSimSwapRulesDto SimSwap { get; set; } = new();
    public TelecomChangeNumberRulesDto ChangeNumber { get; set; } = new();
    public TelecomTakeOverRulesDto TakeOver { get; set; } = new();
    public TelecomDeviceSalesRulesDto DeviceSales { get; set; } = new();
    public TelecomBadDebtRulesDto BadDebt { get; set; } = new();
}

public class TelecomInventoryRulesDto
{
    public int MsisdnQuarantineDays { get; set; } = 90;
    public int SimQuarantineDays { get; set; } = 90;
    public int DormantLineScanIntervalHours { get; set; } = 6;
    public int DormantLineInactivityDays { get; set; } = 365;
    public int MsisdnReservationHours { get; set; } = 24;
    public int MsisdnReservationMinutes { get; set; } = 15;
}

public class TelecomActivationRulesDto
{
    public int MaxLinesIndividual { get; set; } = 5;
    public int MaxLinesCorporate { get; set; } = 50;
    public bool DealerCodeRequired { get; set; } = true;
    public bool RequireKycBeforeConfirm { get; set; } = true;
    public bool RequirePaymentReferenceWhenDepositDue { get; set; } = true;

    /// <summary>Retail / POS channel (enum <c>ActivationChannel.Showroom</c>).</summary>
    public string ChannelShowroomLabelAr { get; set; } = "نقطة البيع";

    public string ChannelShowroomLabelEn { get; set; } = "POS";

    public string ChannelDealerLabelAr { get; set; } = "موزع";

    public string ChannelDealerLabelEn { get; set; } = "Dealer";

    public string ChannelDigitalLabelAr { get; set; } = "رقمي";

    public string ChannelDigitalLabelEn { get; set; } = "Digital";
}

public class TelecomTerminationRulesDto
{
    public decimal DefaultFinalBillAmountSyp { get; set; } = 12_500m;
    public string BackOfficeTypes { get; set; } = "Fraud,Regulatory,Collections";
    public bool CorporateRequiresBackOffice { get; set; } = true;
    public bool VoluntaryRequiresRetentionOutcome { get; set; } = true;
}

public class TelecomRefundRulesDto
{
    public decimal DualApprovalThresholdSyp { get; set; } = 500_000m;
    public string HighRiskMethods { get; set; } = "SyriatelCash";
}

public class TelecomSuspensionRulesDto
{
    public int LongReviewDays { get; set; } = 90;
    public string BackOfficeTypes { get; set; } = "Fraud,Regulatory";
    public bool CorporateRequiresBackOffice { get; set; } = true;
}

public class TelecomSimSwapRulesDto
{
    public bool LostStolenRequiresBackOffice { get; set; } = true;
}

public class TelecomChangeNumberRulesDto
{
    public string PremiumCategories { get; set; } = "Silver,Gold,Platinum";
    public bool BlockQuarantinedTarget { get; set; } = true;
}

public class TelecomTakeOverRulesDto
{
    public bool AlwaysRequiresBackOffice { get; set; } = true;
    public string DefaultDepositTransferPolicy { get; set; } = "TransferToNewOwner";
}

public class TelecomDeviceSalesRulesDto
{
    public int CreditScoreLowThreshold { get; set; } = 550;
    public int CreditScoreGoodThreshold { get; set; } = 650;
    public decimal DownPaymentPercentReject { get; set; } = 100m;
    public decimal DownPaymentPercentVip { get; set; } = 10m;
    public decimal DownPaymentPercentLow { get; set; } = 20m;
    public decimal DownPaymentPercentGood { get; set; } = 15m;
}

public class TelecomBadDebtRulesDto
{
    public int PostpaidBadDebtThresholdMonths { get; set; } = 6;
}
