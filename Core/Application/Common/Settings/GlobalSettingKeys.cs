namespace Application.Common.Settings;

public static class GlobalSettingKeys
{
    public const string MaintenanceMode = "MaintenanceMode";
    public const string MaintenanceMessageAr = "MaintenanceMessageAr";
    public const string MaintenanceMessageEn = "MaintenanceMessageEn";
    public const string MaintenanceBypassIPs = "MaintenanceBypassIPs";
    public const string IsStrictPersonaMode = "IsStrictPersonaMode";
    public const string JwtAccessTokenMinutes = "JwtAccessTokenMinutes";
    public const string IntegrationHuaweiEnabled = "Integration.Huawei.Enabled";
    public const string IntegrationHlrEnabled = "Integration.Hlr.Enabled";
    public const string IntegrationSmsEnabled = "Integration.Sms.Enabled";
    /// <summary>Max active telecom lines per individual customer (regulatory demo default: 5).</summary>
    public const string TelecomMaxActiveLinesPerIndividual = "Telecom.MaxActiveLinesPerIndividual";
}
