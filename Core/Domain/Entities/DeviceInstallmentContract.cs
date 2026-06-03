using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>عقد تقسيط جهاز مرتبط بعملية DEV-.</summary>
public class DeviceInstallmentContract : BaseEntity
{
    public string TelecomOperationRequestId { get; set; } = null!;
    public TelecomOperationRequest? TelecomOperationRequest { get; set; }

    public string ContractNumber { get; set; } = null!;
    public InstallmentContractStatus Status { get; set; } = InstallmentContractStatus.Draft;
    public decimal DownPayment { get; set; }
    public decimal MonthlyAmount { get; set; }
    public int? CreditScoreSnapshot { get; set; }
    public string? DelinquencyStatus { get; set; }
    public string? CbsContractId { get; set; }
    public DateTime? WarrantyStartsAtUtc { get; set; }
    public string? InstallmentPlanId { get; set; }
    public InstallmentPlan? InstallmentPlan { get; set; }

    public ICollection<DeviceInstallmentScheduleLine> ScheduleLines { get; set; } =
        new List<DeviceInstallmentScheduleLine>();
}
