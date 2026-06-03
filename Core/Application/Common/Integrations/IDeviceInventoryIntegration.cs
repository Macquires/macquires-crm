namespace Application.Common.Integrations;

public sealed record DeviceInventoryCheckResult(bool Success, string? MessageAr);

public interface IDeviceInventoryIntegration
{
    Task<DeviceInventoryCheckResult> ValidateImeiAsync(string imei, CancellationToken cancellationToken = default);
    Task<DeviceInventoryCheckResult> ReserveAsync(string imei, string operationId, CancellationToken cancellationToken = default);
    Task<DeviceInventoryCheckResult> ReleaseAsync(string imei, string operationId, CancellationToken cancellationToken = default);
}
