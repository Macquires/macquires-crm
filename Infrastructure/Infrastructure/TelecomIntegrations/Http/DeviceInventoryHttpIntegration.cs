using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations.Http;

public sealed class DeviceInventoryHttpIntegration : IDeviceInventoryIntegration
{
    private readonly DeviceInventoryMockIntegration _fallback;

    public DeviceInventoryHttpIntegration(DeviceInventoryMockIntegration fallback) => _fallback = fallback;

    public Task<DeviceInventoryCheckResult> ValidateImeiAsync(string imei, CancellationToken cancellationToken = default) =>
        _fallback.ValidateImeiAsync(imei, cancellationToken);

    public Task<DeviceInventoryCheckResult> ReserveAsync(
        string imei,
        string operationId,
        CancellationToken cancellationToken = default) =>
        _fallback.ReserveAsync(imei, operationId, cancellationToken);

    public Task<DeviceInventoryCheckResult> ReleaseAsync(
        string imei,
        string operationId,
        CancellationToken cancellationToken = default) =>
        _fallback.ReleaseAsync(imei, operationId, cancellationToken);
}
