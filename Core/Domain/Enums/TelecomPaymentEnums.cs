namespace Domain.Enums;

public enum PaymentTransactionType
{
    Recharge = 0,
    VoucherRedeem = 1,
    BillPay = 2,
    WalletTopUp = 3,
    ActivationDeposit = 4
}

public enum PaymentTransactionStatus
{
    Draft = 0,
    PendingGateway = 1,
    Completed = 2,
    Failed = 3,
    Reversed = 4
}

public enum PaymentServiceChannel
{
    Showroom = 0,
    Digital = 1,
    Dealer = 2
}
