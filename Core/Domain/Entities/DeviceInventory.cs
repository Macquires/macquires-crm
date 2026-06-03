using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>مخزون أجهزة/راوترات — IMEI محكوم (§14).</summary>
public class DeviceInventory : BaseEntity
{
    public string Imei { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string? Sku { get; set; }
    public decimal ListPrice { get; set; }
    public DeviceInventoryStatus Status { get; set; } = DeviceInventoryStatus.Available;
    public string? BranchId { get; set; }
    public string? ReservedByOperationId { get; set; }
    public DateTime? SoldAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }

    private static readonly Dictionary<DeviceInventoryStatus, DeviceInventoryStatus[]> AllowedTransitions = new()
    {
        [DeviceInventoryStatus.Available] = [DeviceInventoryStatus.Reserved, DeviceInventoryStatus.Sold],
        [DeviceInventoryStatus.Reserved] = [DeviceInventoryStatus.Available, DeviceInventoryStatus.Sold],
        [DeviceInventoryStatus.Sold] = [],
        [DeviceInventoryStatus.Quarantined] = [DeviceInventoryStatus.Available],
    };

    public void TransitionTo(DeviceInventoryStatus newStatus)
    {
        if (Status == newStatus) return;

        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"انتقال غير مسموح لحالة الجهاز: {Status} → {newStatus}.");
        }

        Status = newStatus;
        if (newStatus == DeviceInventoryStatus.Sold)
        {
            SoldAtUtc = DateTime.UtcNow;
            ReservedByOperationId = null;
        }

        if (newStatus == DeviceInventoryStatus.Available)
        {
            ReservedByOperationId = null;
        }
    }

    public void ReserveForOperation(string operationId)
    {
        TransitionTo(DeviceInventoryStatus.Reserved);
        ReservedByOperationId = operationId;
    }

    public void ReleaseReservation()
    {
        if (Status == DeviceInventoryStatus.Reserved)
        {
            TransitionTo(DeviceInventoryStatus.Available);
        }
    }
}
