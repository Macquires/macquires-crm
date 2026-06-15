using Domain.Enums;

namespace Application.Common.Telecom;

/// <summary>Allowed <see cref="TelecomOperationStatus"/> transitions for the operation pipeline.</summary>
public static class TelecomOperationLifecycle
{
    private static readonly Dictionary<TelecomOperationStatus, TelecomOperationStatus[]> Allowed = new()
    {
        [TelecomOperationStatus.Draft] =
        [
            TelecomOperationStatus.PendingDocuments,
            TelecomOperationStatus.Failed
        ],
        [TelecomOperationStatus.PendingDocuments] =
        [
            TelecomOperationStatus.Confirmed,
            TelecomOperationStatus.Failed
        ],
        [TelecomOperationStatus.Confirmed] =
        [
            TelecomOperationStatus.Provisioning,
            TelecomOperationStatus.Scheduled,
            TelecomOperationStatus.Failed
        ],
        [TelecomOperationStatus.Scheduled] =
        [
            TelecomOperationStatus.Provisioning,
            TelecomOperationStatus.Failed
        ],
        [TelecomOperationStatus.Provisioning] =
        [
            TelecomOperationStatus.Completed,
            TelecomOperationStatus.Failed,
            TelecomOperationStatus.ProvisioningError,
            TelecomOperationStatus.PendingExternal
        ],
        [TelecomOperationStatus.PendingExternal] =
        [
            TelecomOperationStatus.Provisioning,
            TelecomOperationStatus.Failed,
            TelecomOperationStatus.ProvisioningError
        ],
        [TelecomOperationStatus.ProvisioningError] =
        [
            TelecomOperationStatus.Provisioning,
            TelecomOperationStatus.Failed,
            TelecomOperationStatus.Completed
        ]
    };

    public static bool CanTransition(TelecomOperationStatus from, TelecomOperationStatus to)
    {
        if (from == to) return true;
        return Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    public static void EnsureCanTransition(TelecomOperationStatus from, TelecomOperationStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException($"انتقال غير مسموح لحالة الطلب: {from} → {to}.");
        }
    }

    public static bool IsTerminal(TelecomOperationStatus status) =>
        status is TelecomOperationStatus.Completed or TelecomOperationStatus.Failed;

    public static bool RequiresManualRemediation(TelecomOperationStatus status) =>
        status is TelecomOperationStatus.ProvisioningError;
}
