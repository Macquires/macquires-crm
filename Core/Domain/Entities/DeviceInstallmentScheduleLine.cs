using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class DeviceInstallmentScheduleLine : BaseEntity
{
    public string DeviceInstallmentContractId { get; set; } = null!;
    public DeviceInstallmentContract? DeviceInstallmentContract { get; set; }

    public int Sequence { get; set; }
    public DateTime DueDateUtc { get; set; }
    public decimal Amount { get; set; }
    public InstallmentScheduleLineStatus Status { get; set; } = InstallmentScheduleLineStatus.Pending;
    public DateTime? PaidAtUtc { get; set; }
    public string? PaymentReference { get; set; }
}
