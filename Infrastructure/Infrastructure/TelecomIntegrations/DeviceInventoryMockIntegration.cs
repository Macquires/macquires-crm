using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations;

public sealed class DeviceInventoryMockIntegration : IDeviceInventoryIntegration
{
    public Task<DeviceInventoryCheckResult> ValidateImeiAsync(string imei, CancellationToken cancellationToken = default) =>
        Task.FromResult(new DeviceInventoryCheckResult(true, null));

    public Task<DeviceInventoryCheckResult> ReserveAsync(string imei, string operationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new DeviceInventoryCheckResult(true, null));

    public Task<DeviceInventoryCheckResult> ReleaseAsync(string imei, string operationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new DeviceInventoryCheckResult(true, null));
}
