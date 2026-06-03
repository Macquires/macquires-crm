namespace Domain.Enums;

/// <summary>§15 — refund category on RFD- operations.</summary>
public enum RefundCategory
{
    Deposit = 0,
    WalletBalance = 1,
    Overpayment = 2,
    SyriatelCash = 3
}

/// <summary>§15 — settlement channel.</summary>
public enum RefundSettlementMethod
{
    Cash = 0,
    BankTransfer = 1,
    WalletCredit = 2,
    CreditNote = 3
}

/// <summary>§15 — financial settlement lifecycle (stored as string on entity for reporting).</summary>
public enum RefundSettlementStatus
{
    Pending = 0,
    Settled = 1,
    Failed = 2,
    Reversed = 3
}
