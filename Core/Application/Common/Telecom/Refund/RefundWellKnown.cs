namespace Application.Common.Telecom.Refund;

public static class RefundWellKnown
{
    public const string TypeDeposit = "Deposit";
    public const string TypeWalletBalance = "WalletBalance";
    public const string TypeOverpayment = "Overpayment";
    public const string TypeSyriatelCash = "SyriatelCash";

    public const string MethodCash = "Cash";
    public const string MethodBankTransfer = "BankTransfer";
    public const string MethodWalletCredit = "WalletCredit";
    public const string MethodCreditNote = "CreditNote";

    public const string SettlementPending = "Pending";
    public const string SettlementSettled = "Settled";
    public const string SettlementFailed = "Failed";

    public const decimal DualApprovalThresholdSyp = 500_000m;

    public static bool IsKnownType(string? type)
    {
        var t = (type ?? string.Empty).Trim();
        return t is TypeDeposit or TypeWalletBalance or TypeOverpayment or TypeSyriatelCash;
    }

    public static bool IsKnownMethod(string? method)
    {
        var m = (method ?? string.Empty).Trim();
        return m is MethodCash or MethodBankTransfer or MethodWalletCredit or MethodCreditNote;
    }

    public static decimal MaxRefundableForType(string type, decimal depositSnapshot, decimal walletSnapshot) =>
        type switch
        {
            TypeDeposit => depositSnapshot,
            TypeWalletBalance => walletSnapshot,
            TypeOverpayment => Math.Min(depositSnapshot, walletSnapshot),
            TypeSyriatelCash => walletSnapshot,
            _ => 0m,
        };

    public static bool RequiresBackOfficeApproval(string type, decimal amount, string method) =>
        string.Equals(type, TypeSyriatelCash, StringComparison.OrdinalIgnoreCase)
        || amount > DualApprovalThresholdSyp
        || string.Equals(method, MethodCash, StringComparison.OrdinalIgnoreCase) && amount > DualApprovalThresholdSyp;
}
