using Domain.Enums;

namespace Application.Common.Telecom.DeviceSales;

public sealed record DeviceFinancingMatrixInput(
    int CreditScore,
    bool HasDelinquentContract,
    bool IsNewCustomer,
    bool IsVipSegment);

public sealed record DeviceFinancingMatrixResult(
    DeviceFinancingDecision Decision,
    string ApprovalLevelRequired,
    string NoteAr,
    decimal RequiredDownPaymentPercent);

/// <summary>Device Financing Eligibility Matrix (Executive Design doc §370–395).</summary>
public static class DeviceFinancingDecisionMatrix
{
    public static DeviceFinancingMatrixResult Evaluate(DeviceFinancingMatrixInput input)
    {
        if (input.HasDelinquentContract)
        {
            return new(
                DeviceFinancingDecision.Rejected,
                DeviceSaleWellKnown.ApprovalShowroom,
                "VAL-14-02: يوجد عقد تقسيط متعثر — مرفوض.",
                100m);
        }

        if (input.IsVipSegment)
        {
            return new(
                DeviceFinancingDecision.Conditional,
                DeviceSaleWellKnown.ApprovalManager,
                "استثناء VIP — موافقة مدير مطلوبة.",
                10m);
        }

        if (input.IsNewCustomer || input.CreditScore < 550)
        {
            return new(
                DeviceFinancingDecision.DepositRequired,
                DeviceSaleWellKnown.ApprovalSupervisor,
                "عميل جديد أو سجل ائتماني غير كافٍ — دفعة أولى وموافقة مشرف.",
                20m);
        }

        if (input.CreditScore >= 650)
        {
            return new(
                DeviceFinancingDecision.Approved,
                DeviceSaleWellKnown.ApprovalFinanceOfficer,
                "سجل دفع جيد — اعتماد ضابط مالي.",
                15m);
        }

        return new(
            DeviceFinancingDecision.Approved,
            DeviceSaleWellKnown.ApprovalSupervisor,
            "موافقة مشرف قبل إتمام التقسيط.",
            15m);
    }
}
