namespace Domain.Enums;

public enum DeviceInventoryStatus
{
    Available = 0,
    Reserved = 1,
    Sold = 2,
    Quarantined = 3
}

public enum DeviceSaleType
{
    Cash = 0,
    Installment = 1
}

public enum DeviceFinancingDecision
{
    Pending = 0,
    Approved = 1,
    DepositRequired = 2,
    Rejected = 3,
    Conditional = 4
}

public enum InstallmentContractStatus
{
    Draft = 0,
    Active = 1,
    Delinquent = 2,
    Closed = 3,
    Rejected = 4
}

public enum InstallmentScheduleLineStatus
{
    Pending = 0,
    Paid = 1,
    Overdue = 2
}
