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
    public int JwtAccessTokenMinutes { get; set; } = 60;
    public bool IntegrationHuaweiEnabled { get; set; } = true;
    public bool IntegrationHlrEnabled { get; set; } = true;
    public bool IntegrationSmsEnabled { get; set; } = true;
}
