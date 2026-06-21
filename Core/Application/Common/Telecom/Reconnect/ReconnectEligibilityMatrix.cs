using Application.Common.Telecom.Suspension;
using Domain.Enums;
using Application.Common.Settings;

namespace Application.Common.Telecom.Reconnect;

public sealed record ReconnectEligibilityMatrixInput(
    SubscriberOperationalStatus ProfileOperationalStatus,
    CustomerStatus? CustomerStatus,
    MsisdnPoolStatus MsisdnPoolStatus,
    string? LastSuspensionType,
    string ClearanceType,
    bool HasPaymentReference,
    decimal OutstandingBalance,
    bool FraudClearanceConfirmed,
    string? BdrStatus = null,
    DateTime? SuspensionDate = null);

public sealed record ReconnectEligibilityMatrixResult(
    bool Allowed,
    string MessageAr,
    string ValidationCode,
    bool RequiresBackOfficeApproval);

/// <summary>§9 Reconnect decision matrix (Billing, Fraud, Regulatory, Customer).</summary>
public static class ReconnectEligibilityMatrix
{
    public static ReconnectEligibilityMatrixResult Evaluate(ReconnectEligibilityMatrixInput input)
    {
        // GLOBAL HARDENING: Logic for Revenue Leakage and BDR block
        if (input.LastSuspensionType == SuspensionWellKnown.Billing && input.OutstandingBalance < 0)
        {
            // If BDR is approved and pending cash, we allow reconnect but it will require payment ref in the next step
            if (input.BdrStatus == "Approved_Pending_Cash")
            {
                return new ReconnectEligibilityMatrixResult(
                    true,
                    "VAL-09-02: تم اعتماد طلب تسوية الديون (BDR). يرجى استكمال عملية إعادة التفعيل مع إدخال رقم الوصل المالي.",
                    "BdrApprovedPendingCash",
                    false);
            }

            // DYNAMIC BAD DEBT CALCULATION (BDR Governance Framework)
            var thresholdMonths = GlobalSettings.PostpaidBadDebtThresholdMonths;
            var cutoffDate = DateTime.UtcNow.AddMonths(-thresholdMonths);
            var suspensionDate = input.SuspensionDate ?? DateTime.UtcNow;

            if (suspensionDate < cutoffDate)
            {
                // BAD DEBT: Line suspended longer than threshold (6+ months)
                // ALLOW if Payment Reference provided (payment settles the debt)
                // DENY only if no payment reference (requires BDR process)
                if (!input.HasPaymentReference)
                {
                    return Deny(
                        $"VAL-09-02: تم تجاوز مهلة السداد ({thresholdMonths} شهر). الخط في حالة ديون معدومة (Bad Debt). يرجى تقديم طلب تسوية ديون (BDR Request) عبر المكتب الخلفي المالي.",
                        "BdrPendingBlock");
                }
                // Payment reference provided → payment settles Bad Debt, allow reconnect
            }

            // IF the line has been unpaid/suspended for LESS than the customized number of months -> Overdue Postpaid Debtor
            // Standard synchronous payment at showroom desk is allowed.
        }

        if (input.ProfileOperationalStatus == SubscriberOperationalStatus.Terminated)
        {
            return Deny(
                "VAL-09-05: الخط ملغي نهائياً — يرجى استخدام مسار تفعيل خط جديد.",
                "RequiresNewActivation");
        }

        if (input.CustomerStatus == CustomerStatus.Blacklisted)
        {
            return Deny("VAL-09-01: إعادة التفعيل مرفوضة — العميل على القائمة السوداء.", "Blacklisted");
        }

        if (input.ProfileOperationalStatus == SubscriberOperationalStatus.Active)
        {
            return Deny("VAL-09-01: الخط نشط مسبقاً.", "AlreadyActive");
        }

        if (input.ProfileOperationalStatus is not (
            SubscriberOperationalStatus.Suspended
            or SubscriberOperationalStatus.SuspendedInbound
            or SubscriberOperationalStatus.SuspendedOutbound))
        {
            return Deny(
                $"VAL-09-01: لا يمكن إعادة التفعيل — حالة المشترك {input.ProfileOperationalStatus}.",
                "NotSuspended");
        }

        if (input.MsisdnPoolStatus != MsisdnPoolStatus.Suspended)
        {
            return Deny(
                $"VAL-09-01: حالة الرقم {input.MsisdnPoolStatus} — يتوقع Suspended.",
                "MsisdnNotSuspended");
        }

        var clearance = (input.ClearanceType ?? string.Empty).Trim();

        if (RequiresPaymentClearance(clearance, input.LastSuspensionType))
        {
            if (!input.HasPaymentReference)
            {
                return Deny(
                    string.Equals(clearance, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase)
                        ? "VAL-09-02: مرجع الدفع مطلوب لتسوية حظر الفواتير."
                        : "VAL-09-02: مرجع الدفع مطلوب — الحظر الأصلي مالي (Billing).",
                    "PaymentReferenceRequired");
            }

            // Payment reference supplied — showroom settlement recorded; CBS may lag until back-office sync.
            // Skip live outstanding-balance gate; payment journal is verified at approval (Auto-BDR).
        }

        var requiresBo =
            SuspensionWellKnown.IsBackOfficeType(input.LastSuspensionType)
            || string.Equals(clearance, ReconnectWellKnown.Fraud, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clearance, ReconnectWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase);

        if (requiresBo && !input.FraudClearanceConfirmed)
        {
            return new ReconnectEligibilityMatrixResult(
                true,
                "VAL-09-03: إعادة التفعيل مسموحة — بانتظار اعتماد الباك أوفيس وتأكيد التسوية.",
                "BackOfficePending",
                true);
        }

        if (string.Equals(clearance, ReconnectWellKnown.Operational, StringComparison.OrdinalIgnoreCase))
        {
            return new ReconnectEligibilityMatrixResult(
                true,
                "VAL-09-04: إعادة التفعيل التشغيلية مسموحة — مزامنة فنية فورية (HLR/CRM).",
                "OperationalAllowed",
                false);
        }

        if (string.Equals(clearance, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase))
        {
            // Showroom payment on billing suspension → back-office audit before HLR (Retail cannot Confirm CBS).
            var requiresBoPaymentAudit = input.HasPaymentReference
                && string.Equals(
                    input.LastSuspensionType,
                    SuspensionWellKnown.Billing,
                    StringComparison.OrdinalIgnoreCase);

            return new ReconnectEligibilityMatrixResult(
                true,
                requiresBoPaymentAudit
                    ? "VAL-09-06: تسوية مالية مسجلة — بانتظار اعتماد الباك أوفيس وتدقيق الدفع."
                    : "VAL-09-06: تسوية مالية مكتملة — مسموح إعادة التفعيل.",
                requiresBoPaymentAudit ? "BackOfficePending" : "PaymentCleared",
                requiresBoPaymentAudit);
        }

        return new ReconnectEligibilityMatrixResult(
            true,
            requiresBo
                ? "إعادة التفعيل مسموحة — بانتظار اعتماد الباك أوفيس."
                : "VAL-09-07: مسموح إعادة التفعيل الفوري — نقطة البيع.",
            requiresBo ? "BackOfficePending" : "CustomerAllowed",
            requiresBo);
    }

    /// <summary>Payment rules apply only on financial clearance paths — not Operational/Fraud/Regulatory.</summary>
    private static bool RequiresPaymentClearance(string clearance, string? lastSuspensionType)
    {
        if (string.Equals(clearance, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(clearance, ReconnectWellKnown.Operational, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clearance, ReconnectWellKnown.Fraud, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clearance, ReconnectWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(lastSuspensionType, SuspensionWellKnown.Billing, StringComparison.OrdinalIgnoreCase);
    }

    private static ReconnectEligibilityMatrixResult Deny(
        string messageAr,
        string code,
        bool requiresBackOffice = false) =>
        new(false, messageAr, code, requiresBackOffice);
}
