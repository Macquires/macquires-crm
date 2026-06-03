namespace Application.Common.Integrations;

/// <summary>Named BSS/HLR operations mapped by Infrastructure adapters (Huawei CBS, core HLR).</summary>
public static class TelecomBssOperations
{
    public const string CbsCreateAccountProfile = "CbsCreateAccountProfile";
    public const string CbsPostInitialDeposit = "CbsPostInitialDeposit";
    public const string CbsReverseAccount = "CbsReverseAccount";
    public const string CbsRechargeTopUp = "CbsRechargeTopUp";
    public const string HlrCreateSubscriber = "HlrCreateSubscriber";
    public const string HlrDeactivateSubscriber = "HlrDeactivateSubscriber";
    public const string HlrSimProfileUpdate = "HlrSimProfileUpdate";
    public const string CbsChangeServiceType = "ChangeServiceType";
    public const string CbsTransferOwnership = "CbsTransferOwnership";
    public const string CbsSimProfileUpdate = "CbsSimProfileUpdate";
    public const string HlrChangeGsmType = "ChangeGsmType_ServiceProfile";
    public const string CbsMsisdnReassign = "CbsMsisdnReassign";
    public const string HlrMsisdnUpdate = "HlrMsisdnUpdate";
    public const string CbsGenerateFinalBill = "CbsGenerateFinalBill";
    public const string CbsBarSubscriber = "CbsBarSubscriber";
    public const string CbsUnbarSubscriber = "CbsUnbarSubscriber";
    public const string HlrSuspendSubscriber = "HlrSuspendSubscriber";
    public const string HlrReactivateSubscriber = "HlrReactivateSubscriber";
    public const string CbsChangePrimaryOffer = "CbsChangePrimaryOffer";
    public const string CbsPostDeviceSale = "CbsPostDeviceSale";
    public const string CbsCreateDeviceInstallmentContract = "CbsCreateDeviceInstallmentContract";
    public const string CbsPostRefundCreditNote = "CbsPostRefundCreditNote";
}

/// <summary>Explicit phase for billing provision (idempotency + adapter branching).</summary>
public enum TelecomBillingProvisionPhase
{
    Provision = 0,
    Reverse = 1,
}
