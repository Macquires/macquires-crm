namespace Application.Common.Integrations;

/// <summary>Named BSS/HLR operations mapped by Infrastructure adapters (Huawei CBS, core HLR).</summary>
public static class TelecomBssOperations
{
    public const string CbsCreateAccountProfile = "CbsCreateAccountProfile";
    public const string CbsPostInitialDeposit = "CbsPostInitialDeposit";
    public const string CbsReverseAccount = "CbsReverseAccount";
    public const string HlrCreateSubscriber = "HlrCreateSubscriber";
    public const string HlrDeactivateSubscriber = "HlrDeactivateSubscriber";
    public const string HlrSimProfileUpdate = "HlrSimProfileUpdate";
}

/// <summary>Explicit phase for billing provision (idempotency + adapter branching).</summary>
public enum TelecomBillingProvisionPhase
{
    Provision = 0,
    Reverse = 1,
}
