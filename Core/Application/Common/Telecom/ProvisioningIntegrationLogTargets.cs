namespace Application.Common.Telecom;

/// <summary>Integration target labels stored on <see cref="Domain.Entities.BillingIntegrationLog"/> rows.</summary>
public static class ProvisioningIntegrationLogTargets
{
    public const string CbsMock = "HuaweiCBS-Mock";
    public const string CbsApi = "Huawei CBS API v2.1";
    public const string InMock = "Huawei-IN-Mock";
    public const string HlrMock = "HLR-Mock";
    public const string HlrVasMock = "HLR-VAS-Mock";
    public const string HlrHssMock = "Huawei-HLR-Mock";

    public static readonly string[] BillingTargets = [CbsMock, CbsApi];

    public static readonly string[] InTargets = [InMock];

    public static readonly string[] HlrTargets = [HlrMock, HlrVasMock, HlrHssMock];
}
