using Domain.Enums;

namespace Application.Common.Telecom;

public static class TechnicalTicketCategoryMapper
{
    public static TechnicalTicketCategory FromTelecomOperationKind(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.SimSwap => TechnicalTicketCategory.SimSwap,
        TelecomOperationKind.Migration => TechnicalTicketCategory.PackageMigration,
        TelecomOperationKind.TakeOver => TechnicalTicketCategory.OwnershipTransfer,
        TelecomOperationKind.NewActivation => TechnicalTicketCategory.LineActivation,
        TelecomOperationKind.ServiceModification => TechnicalTicketCategory.VasActivation,
        TelecomOperationKind.ChangeGsmType => TechnicalTicketCategory.ChangeGsmType,
        _ => TechnicalTicketCategory.Complaint,
    };

    public static TechnicalTicketIssueType DefaultIssueType(TechnicalTicketCategory category) => category switch
    {
        TechnicalTicketCategory.SimSwap => TechnicalTicketIssueType.SimBlock,
        TechnicalTicketCategory.PackageMigration => TechnicalTicketIssueType.Billing,
        TechnicalTicketCategory.OwnershipTransfer => TechnicalTicketIssueType.Provisioning,
        TechnicalTicketCategory.LineActivation => TechnicalTicketIssueType.Provisioning,
        TechnicalTicketCategory.VasActivation => TechnicalTicketIssueType.Provisioning,
        TechnicalTicketCategory.FraudPayment => TechnicalTicketIssueType.Billing,
        _ => TechnicalTicketIssueType.Network,
    };

    public static TechnicalTicketPriority DefaultPriority(TechnicalTicketCategory category) => category switch
    {
        TechnicalTicketCategory.SimSwap => TechnicalTicketPriority.High,
        TechnicalTicketCategory.LineActivation => TechnicalTicketPriority.High,
        TechnicalTicketCategory.FraudPayment => TechnicalTicketPriority.Critical,
        TechnicalTicketCategory.OwnershipTransfer => TechnicalTicketPriority.Medium,
        _ => TechnicalTicketPriority.Medium,
    };

    public static string CategoryLabelAr(TechnicalTicketCategory category) => category switch
    {
        TechnicalTicketCategory.SimSwap => "تبديل شريحة",
        TechnicalTicketCategory.PackageMigration => "ترحيل باقة",
        TechnicalTicketCategory.OwnershipTransfer => "نقل ملكية",
        TechnicalTicketCategory.LineActivation => "تفعيل خط",
        TechnicalTicketCategory.VasActivation => "إضافة خدمة / VAS",
        TechnicalTicketCategory.FraudPayment => "احتيال شحن / دفع",
        _ => "شكوى",
    };
}
