using Domain.Enums;

namespace Application.Common.Telecom.BadDebt;

public sealed record BadDebtEligibilityMatrixInput(
    SubscriberOperationalStatus ProfileOperationalStatus,
    CustomerStatus? CustomerStatus,
    string CollectionAction,
    string? RequestedDunningStage,
    string? PriorDunningStage,
    bool HasPaymentReference,
    decimal? CollectedAmount,
    decimal? WriteOffAmount,
    decimal OutstandingBalance,
    bool CollectionApprovalConfirmed);

public sealed record BadDebtEligibilityMatrixResult(
    bool Allowed,
    string MessageAr,
    string ValidationCode,
    bool RequiresBackOfficeApproval);

/// <summary>§16 Collections / Bad Debt Recovery decision matrix (VAL-16).</summary>
public static class BadDebtEligibilityMatrix
{
    public static BadDebtEligibilityMatrixResult Evaluate(BadDebtEligibilityMatrixInput input)
    {
        if (input.ProfileOperationalStatus == SubscriberOperationalStatus.Terminated)
        {
            return Deny(
                "VAL-16-01: الخط ملغي نهائياً — لا يمكن فتح مسار تحصيل على خط منتهٍ.",
                "Terminated");
        }

        if (input.CustomerStatus == CustomerStatus.Blacklisted)
        {
            return Deny("VAL-16-03: التحصيل مرفوض — العميل على القائمة السوداء.", "Blacklisted");
        }

        if (input.OutstandingBalance >= 0)
        {
            return Deny(
                "VAL-16-02: لا توجد ذمة مالية سالبة على الخط — مسار التحصيل غير مطلوب.",
                "NoOutstandingDebt");
        }

        var action = (input.CollectionAction ?? string.Empty).Trim();

        if (string.Equals(action, BadDebtWellKnown.PaymentRecorded, StringComparison.OrdinalIgnoreCase))
        {
            if (!input.HasPaymentReference)
            {
                return Deny("VAL-16-06: مرجع الدفع مطلوب لتسجيل التحصيل.", "PaymentReferenceRequired");
            }

            if (input.CollectedAmount is not > 0)
            {
                return Deny("VAL-16-06: مبلغ التحصيل يجب أن يكون أكبر من صفر.", "CollectedAmountRequired");
            }
        }

        if (string.Equals(action, BadDebtWellKnown.WriteOffPartial, StringComparison.OrdinalIgnoreCase)
            || string.Equals(action, BadDebtWellKnown.WriteOffFull, StringComparison.OrdinalIgnoreCase))
        {
            if (input.WriteOffAmount is not > 0)
            {
                return Deny("VAL-16-06: مبلغ الشطب مطلوب.", "WriteOffAmountRequired");
            }
        }

        if (string.Equals(input.RequestedDunningStage, BadDebtWellKnown.HardBar, StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, BadDebtWellKnown.DunningEscalation, StringComparison.OrdinalIgnoreCase))
        {
            var prior = (input.PriorDunningStage ?? BadDebtWellKnown.Reminder1).Trim();
            var priorOk = string.Equals(prior, BadDebtWellKnown.SoftBar, StringComparison.OrdinalIgnoreCase)
                          || string.Equals(prior, BadDebtWellKnown.Reminder2, StringComparison.OrdinalIgnoreCase)
                          || string.Equals(prior, BadDebtWellKnown.HardBar, StringComparison.OrdinalIgnoreCase);

            if (!priorOk)
            {
                return Deny(
                    "VAL-16-07: الحظر الصلب يتطلب تصعيداً عبر SoftBar أو Reminder2 أولاً.",
                    "HardBarSequenceRequired");
            }
        }

        if (BadDebtWellKnown.RequiresBackOffice(action))
        {
            if (!input.CollectionApprovalConfirmed)
            {
                return new BadDebtEligibilityMatrixResult(
                    true,
                    "VAL-16-05: الإجراء مسموح — بانتظار اعتماد الإدارة المالية (باك أوفيس).",
                    "BackOfficePending",
                    true);
            }

            return new BadDebtEligibilityMatrixResult(
                true,
                "مسموح — بانتظار تنفيذ اعتماد الشطب/الوكالة.",
                "BackOfficeApproved",
                true);
        }

        return new BadDebtEligibilityMatrixResult(
            true,
            "مسموح تسجيل التحصيل/التصعيد من نقطة البيع.",
            "Allowed",
            false);
    }

    private static BadDebtEligibilityMatrixResult Deny(string messageAr, string code) =>
        new(false, messageAr, code, false);
}
