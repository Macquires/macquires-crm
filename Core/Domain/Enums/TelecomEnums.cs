namespace Domain.Enums;

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
  /// <summary>Confirmed locally by agent (pre-provisioning).</summary>
    Confirmed = 1,
  /// <summary>Deferred external retry / queue.</summary>
    PendingExternal = 2,
    Completed = 3,
    Failed = 4,
  /// <summary>Documents uploaded; awaiting confirmation.</summary>
    PendingDocuments = 5,
  /// <summary>Provisioning in progress (CBS/HLR).</summary>
    Provisioning = 6
}

public enum SubscriberType
{
    Individual = 0,
    Corporate = 1
}
