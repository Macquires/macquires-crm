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
        _ => status.ToString()
    };
}
