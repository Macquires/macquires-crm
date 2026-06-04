using Application.Common.Telecom.Suspension;
using Domain.Enums;

namespace Application.Common.Telecom.Reconnect;

public sealed record ReconnectEligibilityMatrixInput(
    SubscriberOperationalStatus ProfileOperationalStatus,
    CustomerStatus? CustomerStatus,
    MsisdnPoolStatus MsisdnPoolStatus,
    string? LastSuspensionType,
    string ClearanceType,
    bool HasPaymentReference,
    decimal OutstandingBalance,
    bool FraudClearanceConfirmed);

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
        var requiresPaymentClearance =
            string.Equals(input.LastSuspensionType, SuspensionWellKnown.Billing, StringComparison.OrdinalIgnoreCase)
            || string.Equals(clearance, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase);

        if (requiresPaymentClearance)
        {
            if (!input.HasPaymentReference)
            {
                return Deny(
                    "VAL-09-02: مرجع الدفع مطلوب لتسوية حظر الفواتير.",
                    "PaymentReferenceRequired");
            }

            if (input.OutstandingBalance < 0)
            {
                return Deny(
                    $"VAL-09-02: ذمم مالية بقيمة {-input.OutstandingBalance:N0} ل.س — يجب التسوية قبل إعادة التفعيل.",
                    "OutstandingDebt");
            }
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

        return new ReconnectEligibilityMatrixResult(
            true,
            requiresBo
                ? "إعادة التفعيل مسموحة — بانتظار اعتماد الباك أوفيس."
                : "مسموح إعادة التفعيل الفوري — معرض.",
            requiresBo ? "BackOfficePending" : "Allowed",
            requiresBo);
    }

    private static ReconnectEligibilityMatrixResult Deny(
        string messageAr,
        string code,
        bool requiresBackOffice = false) =>
        new(false, messageAr, code, requiresBackOffice);
}
