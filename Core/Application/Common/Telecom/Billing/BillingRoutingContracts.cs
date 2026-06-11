using Domain.Enums;

namespace Application.Common.Telecom.Billing;

/// <summary>Resolved subscriber billing persona for convergent routing.</summary>
public enum BillingLineType
{
    Prepaid = 0,
    Postpaid = 1,
    Hybrid = 2,
}

/// <summary>Which financial platforms must participate in an operation.</summary>
[Flags]
public enum BillingRoutingChannel
{
    None = 0,
    In = 1,
    Cbs = 2,
}

public sealed record BillingRoutingDecision(
    BillingLineType LineType,
    BillingRoutingChannel Channels,
    bool CbsBeforeIn,
    string Rationale)
{
    public bool UsesIn => Channels.HasFlag(BillingRoutingChannel.In);
    public bool UsesCbs => Channels.HasFlag(BillingRoutingChannel.Cbs);
}

public static class BillingLineTypeResolver
{
    public static BillingLineType FromCode(string? subscriptionTypeCode)
    {
        if (IsHybrid(subscriptionTypeCode))
        {
            return BillingLineType.Hybrid;
        }

        if (IsPostpaid(subscriptionTypeCode))
        {
            return BillingLineType.Postpaid;
        }

        return BillingLineType.Prepaid;
    }

    public static bool IsPrepaid(string? subscriptionTypeCode) =>
        string.Equals(subscriptionTypeCode, "PREPAID", StringComparison.OrdinalIgnoreCase);

    public static bool IsPostpaid(string? subscriptionTypeCode) =>
        string.Equals(subscriptionTypeCode, "POSTPAID", StringComparison.OrdinalIgnoreCase);

    public static bool IsHybrid(string? subscriptionTypeCode) =>
        string.Equals(subscriptionTypeCode, "HYBRID", StringComparison.OrdinalIgnoreCase);

    public static BillingRoutingDecision ResolveProvisionRouting(
        TelecomOperationKind kind,
        string? subscriptionTypeCode,
        string? sourceSubscriptionTypeCode = null,
        string? targetSubscriptionTypeCode = null)
    {
        var lineType = FromCode(subscriptionTypeCode);

        return kind switch
        {
            TelecomOperationKind.NewActivation => lineType switch
            {
                BillingLineType.Prepaid => new(
                    lineType,
                    BillingRoutingChannel.In,
                    false,
                    "Prepaid activation — real-time IN provisioning and wallet credit."),
                BillingLineType.Postpaid => new(
                    lineType,
                    BillingRoutingChannel.Cbs,
                    false,
                    "Postpaid activation — CBS account profile and deposit ledger."),
                BillingLineType.Hybrid => new(
                    lineType,
                    BillingRoutingChannel.Cbs | BillingRoutingChannel.In,
                    true,
                    "Hybrid activation — CBS master ledger then IN quota sync."),
                _ => new(lineType, BillingRoutingChannel.Cbs, false, "Default CBS provisioning."),
            },

            TelecomOperationKind.Migration => lineType switch
            {
                BillingLineType.Prepaid => new(
                    lineType,
                    BillingRoutingChannel.In,
                    false,
                    "Prepaid offer migration — debit IN wallet before HLR signaling."),
                BillingLineType.Postpaid => new(
                    lineType,
                    BillingRoutingChannel.Cbs,
                    false,
                    "Postpaid offer migration — CBS primary offer change."),
                BillingLineType.Hybrid => new(
                    lineType,
                    BillingRoutingChannel.Cbs | BillingRoutingChannel.In,
                    true,
                    "Hybrid offer migration — CBS ledger then IN quota push."),
                _ => new(lineType, BillingRoutingChannel.Cbs, false, "Default CBS migration."),
            },

            TelecomOperationKind.ChangeGsmType => new(
                FromCode(targetSubscriptionTypeCode ?? subscriptionTypeCode),
                BillingRoutingChannel.Cbs | BillingRoutingChannel.In,
                IsPrepaidToPostpaid(sourceSubscriptionTypeCode, targetSubscriptionTypeCode),
                "GSM type change — liquidate IN wallet when upgrading to postpaid, CBS profile update."),

            TelecomOperationKind.Termination => lineType switch
            {
                BillingLineType.Prepaid => new(
                    lineType,
                    BillingRoutingChannel.In,
                    false,
                    "Prepaid termination — halt IN rating; no CBS final bill."),
                BillingLineType.Postpaid => new(
                    lineType,
                    BillingRoutingChannel.Cbs,
                    false,
                    "Postpaid termination — CBS final bill before quarantine."),
                BillingLineType.Hybrid => new(
                    lineType,
                    BillingRoutingChannel.Cbs | BillingRoutingChannel.In,
                    true,
                    "Hybrid termination — CBS final bill then IN lifecycle halt."),
                _ => new(lineType, BillingRoutingChannel.Cbs, false, "Default CBS termination."),
            },

            _ => new(
                lineType,
                BillingRoutingChannel.Cbs,
                false,
                "Operational profile change — CBS convergent ledger."),
        };
    }

    public static bool IsPrepaidToPostpaid(string? sourceCode, string? targetCode) =>
        IsPrepaid(sourceCode) && IsPostpaid(targetCode);

    public static string IntegrationFalloutSource(BillingRoutingDecision decision) =>
        decision.UsesIn && !decision.UsesCbs ? "IN" : "CBS";
}
