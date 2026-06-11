using Application.Common.Settings.Telecom;

namespace Application.Common.Settings;

public interface IGlobalSettingsAdminService
{
    Task<GlobalSettingsSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task SaveSnapshotAsync(GlobalSettingsSnapshotDto snapshot, string? updatedById, CancellationToken cancellationToken = default);
}

public class GlobalSettingsSnapshotDto
{
    public bool MaintenanceMode { get; set; }
    public string? MaintenanceMessageAr { get; set; }
    public string? MaintenanceMessageEn { get; set; }
    public string? MaintenanceBypassIPs { get; set; }
    public bool IsStrictPersonaMode { get; set; }
    public bool IsDemoVersion { get; set; } = true;
    public int JwtAccessTokenMinutes { get; set; } = 60;
    public bool IntegrationHuaweiEnabled { get; set; } = true;
    public bool IntegrationHlrEnabled { get; set; } = true;
    public bool IntegrationSmsEnabled { get; set; } = true;
    public bool IntegrationInEnabled { get; set; } = true;
    public string? IntegrationInGatewayUrl { get; set; }
    public int IntegrationInTimeoutMilliseconds { get; set; } = 5000;
    public bool IntegrationInSimulateRealTimeDeduction { get; set; } = true;
    public bool NotificationSmsCustomerOpsEnabled { get; set; } = true;
    public bool NotificationSmsWelcomeEnabled { get; set; } = true;
    public string? NotificationSmsWelcomeTemplateAr { get; set; }
    public string? NotificationSmsWelcomeTemplateEn { get; set; }

    /// <summary>
    /// قواعد أعمال Telecom. إذا null عند الحفظ، تُحافظ القيم الموجودة في DB (merge آمن).
    /// </summary>
    public TelecomBusinessRulesDto? Telecom { get; set; }
}
