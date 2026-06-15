using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.ChangeGsm;
using Application.Common.Telecom.ChangeNumber;
using Application.Common.Telecom.OfferSubscription;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.Refund;
using Application.Common.Telecom.SimSwap;
using Application.Common.Telecom.Suspension;
using Application.Common.Telecom.TakeOver;
using Application.Common.Telecom.Termination;

namespace Application.Common.Telecom.OperationCreate;

public static class OperationCreateAuditHelpers
{
    public static string? AppendChangeGsmAudit(string? notes, ChangeGsmEligibilityResult eligibility)
    {
        var stamp =
            $"CGT|src={eligibility.SourceTypeCode ?? eligibility.SourceSubscriptionTypeId}|tgt={eligibility.TargetTypeCode}|status={eligibility.CompatibilityStatus}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendTakeOverAudit(string? notes, TakeOverEligibilityResult eligibility)
    {
        var stamp = $"TKO|msisdn={eligibility.Msisdn ?? "—"}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendSimSwapAudit(string? notes, SimSwapEligibilityResult eligibility)
    {
        var stamp = $"SIM|msisdn={eligibility.Msisdn ?? "—"}|prior={eligibility.PriorSimInventoryId ?? "—"}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendChangeNumberAudit(string? notes, ChangeNumberEligibilityResult eligibility)
    {
        var stamp =
            $"CNR|cur={eligibility.CurrentMsisdn ?? "—"}|tgt={eligibility.TargetMsisdn ?? "—"}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendPortInAudit(
        string? notes,
        ChangeNumberEligibilityResult eligibility,
        Domain.Entities.TelecomOperationRequest entity)
    {
        var stamp =
            $"MNP|cur={eligibility.CurrentMsisdn ?? "—"}|portIn={entity.PortInMsisdn ?? eligibility.TargetMsisdn ?? "—"}|donor={entity.DonorOperatorCode ?? "—"}|ref={entity.AgencyReference ?? "—"}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendTerminationAudit(string? notes, TerminationEligibilityResult eligibility)
    {
        var stamp =
            $"TRM|msisdn={eligibility.Msisdn ?? "—"}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendOfferMigrationAudit(string? notes, OfferSubscriptionEligibilityResult eligibility)
    {
        var stamp =
            $"MGR|msisdn={eligibility.Msisdn ?? "—"}|priorProduct={eligibility.PriorProductId ?? "—"}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendRefundAudit(string? notes, RefundEligibilityResult eligibility)
    {
        var stamp =
            $"RFD|msisdn={eligibility.Msisdn ?? "—"}|deposit={eligibility.DepositBalanceSnapshot:N0}|wallet={eligibility.WalletBalanceSnapshot:N0}|dual={eligibility.RequiresDualApproval}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.OutcomeCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendSuspensionAudit(string? notes, SuspensionEligibilityResult eligibility)
    {
        var stamp =
            $"SUS|msisdn={eligibility.Msisdn ?? "—"}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendReconnectAudit(string? notes, ReconnectEligibilityResult eligibility)
    {
        var stamp =
            $"RCN|msisdn={eligibility.Msisdn ?? "—"}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    public static string? AppendBadDebtAudit(string? notes, BadDebtEligibilityResult eligibility)
    {
        var stamp =
            $"BDR|msisdn={eligibility.Msisdn ?? "—"}|balance={eligibility.OutstandingBalanceSnapshot:N0}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }
}
