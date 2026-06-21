using Application.Common.Audit;
using Application.Common.Telecom;
using Application.Features.CustomerManager.Queries;
using Application.Features.TelecomManager.Queries;
using Domain.Enums;

namespace Application.Common.Telecom.Customer360;

public static class Customer360TimelineComposer
{
    private static readonly HashSet<string> ExcludedAuditActions =
    [
        UserAuditActionTypes.CustomerViewed,
        UserAuditActionTypes.SubscriberSearched,
    ];

    public static bool ShouldIncludeAudit(string actionType) =>
        !ExcludedAuditActions.Contains(actionType);

    public static Customer360TimelineItemDto MapOperation(
        TelecomOperationKind kind,
        TelecomOperationStatus status,
        string? number,
        string? msisdn,
        DateTime? createdAtUtc,
        string operationId)
    {
        var statusAr = TelecomOperationLabels.StatusLabelAr(status);
        var statusEn = status.ToString();
        return new Customer360TimelineItemDto
        {
            OccurredAtUtc = createdAtUtc ?? DateTime.MinValue,
            Kind = Customer360TimelineKind.Operation,
            TitleAr = TelecomOperationLabels.KindLabelAr(kind),
            TitleEn = TelecomOperationLabels.KindLabelEn(kind),
            Subtitle = BuildLineReference(number, msisdn),
            Status = statusAr,
            ReferenceId = string.IsNullOrWhiteSpace(number) ? null : number.Trim(),
            ActionUrl = $"/Telecom/TelecomHub?operationId={operationId}",
        };
    }

    public static Customer360TimelineItemDto MapPayment(
        PaymentTransactionType transactionType,
        PaymentTransactionStatus status,
        decimal amount,
        string? msisdn,
        string? number,
        DateTime? createdAtUtc)
    {
        return new Customer360TimelineItemDto
        {
            OccurredAtUtc = createdAtUtc ?? DateTime.MinValue,
            Kind = Customer360TimelineKind.Payment,
            TitleAr = PaymentTypeLabelAr(transactionType),
            TitleEn = PaymentTypeLabelEn(transactionType),
            Subtitle = $"{amount:N0} ل.س · الخط {msisdn ?? "—"}",
            Status = PaymentStatusLabelAr(status),
            ReferenceId = string.IsNullOrWhiteSpace(number) ? null : number.Trim(),
        };
    }

    public static Customer360TimelineItemDto MapTicket(
        string ticketNumber,
        TechnicalTicketStatus status,
        TechnicalTicketPriority priority,
        TechnicalTicketIssueType issueType,
        string? msisdn,
        string? notes,
        DateTime? createdAtUtc)
    {
        var issue = TicketIssueLabelAr(issueType);
        var detailParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(msisdn))
        {
            detailParts.Add($"الخط {msisdn.Trim()}");
        }

        detailParts.Add(issue);
        if (!string.IsNullOrWhiteSpace(notes))
        {
            detailParts.Add(notes.Trim());
        }

        return new Customer360TimelineItemDto
        {
            OccurredAtUtc = createdAtUtc ?? DateTime.MinValue,
            Kind = Customer360TimelineKind.Ticket,
            TitleAr = $"تذكرة {ticketNumber}",
            TitleEn = $"Ticket {ticketNumber}",
            Subtitle = string.Join(" · ", detailParts),
            Status = TicketStatusLabelAr(status),
            ReferenceId = ticketNumber,
            ActionUrl = "/Telecom/TechnicalTicketList",
        };
    }

    public static Customer360TimelineItemDto MapBilling(GetBillingIntegrationLogListDto log)
    {
        var target = ShortIntegrationTarget(log.IntegrationTarget);
        var message = string.IsNullOrWhiteSpace(log.Message)
            ? (log.Success ? "تمت المزامنة بنجاح" : "فشلت المزامنة")
            : log.Message.Trim();

        return new Customer360TimelineItemDto
        {
            OccurredAtUtc = log.CreatedAtUtc ?? DateTime.MinValue,
            Kind = Customer360TimelineKind.Billing,
            TitleAr = $"مزامنة {target}",
            TitleEn = $"{target} sync",
            Subtitle = message,
            Status = log.Success ? "نجاح" : "فشل",
            ReferenceId = string.IsNullOrWhiteSpace(log.OperationNumber) ? null : log.OperationNumber.Trim(),
        };
    }

    public static Customer360TimelineItemDto MapAudit(UserAuditLogListItemDto audit)
    {
        var actor = FormatActorName(audit.ActorDisplayName);
        var titleAr = !string.IsNullOrWhiteSpace(audit.SummaryAr)
            ? audit.SummaryAr.Trim()
            : AuditActionLabelAr(audit.ActionType);

        return new Customer360TimelineItemDto
        {
            OccurredAtUtc = audit.OccurredAtUtc,
            Kind = Customer360TimelineKind.Audit,
            TitleAr = titleAr,
            TitleEn = AuditActionLabelEn(audit.ActionType),
            Subtitle = $"بواسطة {actor}",
            Status = null,
            ReferenceId = null,
        };
    }

    private static string? BuildLineReference(string? number, string? msisdn)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(msisdn))
        {
            parts.Add($"الخط {msisdn.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(number))
        {
            parts.Add($"مرجع {number.Trim()}");
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : null;
    }

    private static string ShortIntegrationTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return "CBS";
        }

        return target switch
        {
            var t when t.Contains("CBS", StringComparison.OrdinalIgnoreCase) => "CBS",
            var t when t.Contains("HLR", StringComparison.OrdinalIgnoreCase) => "HLR",
            var t when t.Contains("IN", StringComparison.OrdinalIgnoreCase) => "IN",
            _ => target.Trim(),
        };
    }

    private static string FormatActorName(string? actorDisplayName)
    {
        if (string.IsNullOrWhiteSpace(actorDisplayName))
        {
            return "النظام";
        }

        return string.Equals(actorDisplayName, "system-seed", StringComparison.OrdinalIgnoreCase)
            ? "بيانات الديمو"
            : actorDisplayName.Trim();
    }

    private static string PaymentTypeLabelAr(PaymentTransactionType type) => type switch
    {
        PaymentTransactionType.Recharge => "شحن رصيد",
        PaymentTransactionType.VoucherRedeem => "استرداد قسيمة",
        PaymentTransactionType.BillPay => "دفع فاتورة",
        PaymentTransactionType.WalletTopUp => "تعبئة محفظة",
        PaymentTransactionType.ActivationDeposit => "عربون تفعيل",
        _ => "دفعة",
    };

    private static string PaymentTypeLabelEn(PaymentTransactionType type) => type switch
    {
        PaymentTransactionType.Recharge => "Balance recharge",
        PaymentTransactionType.VoucherRedeem => "Voucher redeem",
        PaymentTransactionType.BillPay => "Bill payment",
        PaymentTransactionType.WalletTopUp => "Wallet top-up",
        PaymentTransactionType.ActivationDeposit => "Activation deposit",
        _ => "Payment",
    };

    private static string PaymentStatusLabelAr(PaymentTransactionStatus status) => status switch
    {
        PaymentTransactionStatus.Completed => "مكتمل",
        PaymentTransactionStatus.PendingGateway => "بانتظار البوابة",
        PaymentTransactionStatus.Failed => "فشل",
        PaymentTransactionStatus.Reversed => "معكوس",
        PaymentTransactionStatus.Draft => "مسودة",
        _ => status.ToString(),
    };

    private static string TicketStatusLabelAr(TechnicalTicketStatus status) => status switch
    {
        TechnicalTicketStatus.Open => "مفتوحة",
        TechnicalTicketStatus.InProgress => "قيد المعالجة",
        TechnicalTicketStatus.Resolved => "مغلقة",
        TechnicalTicketStatus.Escalated => "مصعّدة",
        _ => status.ToString(),
    };

    private static string TicketIssueLabelAr(TechnicalTicketIssueType issue) => issue switch
    {
        TechnicalTicketIssueType.Network => "شبكة",
        TechnicalTicketIssueType.Billing => "فوترة",
        TechnicalTicketIssueType.SimBlock => "حظر SIM",
        TechnicalTicketIssueType.Provisioning => "تفعيل/تزويد",
        _ => issue.ToString(),
    };

    private static string AuditActionLabelAr(string actionType) => actionType switch
    {
        UserAuditActionTypes.CustomerCreated => "إنشاء ملف مشترك",
        UserAuditActionTypes.CustomerUpdated => "تحديث بيانات المشترك",
        UserAuditActionTypes.TelecomOperationConfirmed => "تأكيد عملية BSS",
        UserAuditActionTypes.BackOfficeTelecomApproved => "اعتماد باك أوفيس",
        UserAuditActionTypes.BackOfficeTelecomRejected => "رفض باك أوفيس",
        UserAuditActionTypes.BackOfficeTicketClaimed => "استلام تذكرة",
        UserAuditActionTypes.TicketResolved => "إغلاق تذكرة",
        _ => actionType,
    };

    private static string AuditActionLabelEn(string actionType) => actionType switch
    {
        UserAuditActionTypes.CustomerCreated => "Customer created",
        UserAuditActionTypes.CustomerUpdated => "Customer updated",
        UserAuditActionTypes.TelecomOperationConfirmed => "BSS operation confirmed",
        UserAuditActionTypes.TicketResolved => "Ticket resolved",
        _ => actionType,
    };
}
