namespace Domain.Enums;

public enum TelecomSubscriptionType
{
    Prepaid = 0,
    Postpaid = 1,
    Hybrid = 2
}

public enum MsisdnPoolStatus
{
    Available = 0,
    Reserved = 1,
    Active = 2,
    Suspended = 3,
    Quarantined = 4
}

public enum TelecomDocumentStatus
{
    Missing = 0,
    Uploaded = 1,
    Verified = 2,
    Rejected = 3
}

public enum TelecomOperationKind
{
    NewActivation = 0,
    Migration = 1,
    TakeOver = 2,
    SimSwap = 3,
    ServiceModification = 4,
    NumberPortability = 5
}

public enum TelecomOperationStatus
{
    Draft = 0,
    Confirmed = 1,
    PendingExternal = 2,
    Completed = 3,
    Failed = 4
}
