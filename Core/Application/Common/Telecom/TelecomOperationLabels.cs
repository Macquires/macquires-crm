using Domain.Enums;

namespace Application.Common.Telecom;

public static class TelecomOperationLabels
{
    public static string KindLabelAr(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.NewActivation => "تفعيل خط",
        TelecomOperationKind.Migration => "تحويل باقة",
        TelecomOperationKind.TakeOver => "نقل ملكية",
        TelecomOperationKind.SimSwap => "تبديل شريحة",
        TelecomOperationKind.ServiceModification => "خدمة VAS",
        TelecomOperationKind.NumberPortability => "تغيير رقم CNR",
        TelecomOperationKind.ChangeGsmType => "تحويل نوع الخط CGT",
        TelecomOperationKind.Termination => "إنهاء خط TRM",
        TelecomOperationKind.TemporarySuspension => "حظر مؤقت SUS",
        TelecomOperationKind.Reconnect => "إعادة تفعيل RCN",
        TelecomOperationKind.DeviceSale => "بيع جهاز DEV",
        TelecomOperationKind.DepositRefundSettlement => "استرداد مالي RFD",
        TelecomOperationKind.BadDebtRecovery => "تحصيل ديون BDR",
        _ => kind.ToString()
    };

    public static string KindLabelEn(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.NewActivation => "New activation",
        TelecomOperationKind.Migration => "Package migration",
        TelecomOperationKind.TakeOver => "Ownership transfer",
        TelecomOperationKind.SimSwap => "SIM swap",
        TelecomOperationKind.ServiceModification => "VAS service",
        TelecomOperationKind.NumberPortability => "Change MSISDN (CNR)",
        TelecomOperationKind.ChangeGsmType => "Line technology (CGT)",
        TelecomOperationKind.Termination => "Line termination (TRM)",
        TelecomOperationKind.TemporarySuspension => "Temporary suspension (SUS)",
        TelecomOperationKind.Reconnect => "Reconnection (RCN)",
        TelecomOperationKind.DeviceSale => "Device sale (DEV)",
        TelecomOperationKind.DepositRefundSettlement => "Refund (RFD)",
        TelecomOperationKind.BadDebtRecovery => "Bad debt recovery (BDR)",
        _ => kind.ToString()
    };

    public static string StatusLabelAr(TelecomOperationStatus status) => status switch
    {
        TelecomOperationStatus.Draft => "مسودة",
        TelecomOperationStatus.PendingDocuments => "قيد التدقيق القانوني",
        TelecomOperationStatus.Confirmed => "مؤكد محلياً",
        TelecomOperationStatus.Provisioning => "تجهيز الشبكة",
        TelecomOperationStatus.Completed => "منجز",
        TelecomOperationStatus.Failed => "فشل",
        TelecomOperationStatus.PendingExternal => "مزامنة خارجية",
        TelecomOperationStatus.Scheduled => "مجدول",
        TelecomOperationStatus.ProvisioningError => "خطأ تزويد",
        TelecomOperationStatus.Approved_Pending_Cash => "معتمد — بانتظار التحصيل",
        TelecomOperationStatus.Paid_Pending_BackOffice_Clearance => "مدفوع — بانتظار تدقيق الباك أوفيس",
        TelecomOperationStatus.In_Progress => "قيد المعالجة",
        _ => status.ToString()
    };
}
