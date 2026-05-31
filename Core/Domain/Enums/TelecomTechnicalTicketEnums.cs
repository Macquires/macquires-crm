namespace Domain.Enums;

/// <summary>Unified back-office operations queue category (wizard + call center).</summary>
public enum TechnicalTicketCategory
{
    Complaint = 0,
    SimSwap = 1,
    PackageMigration = 2,
    OwnershipTransfer = 3,
    LineActivation = 4,
    VasActivation = 5,
}

public enum TechnicalTicketIssueType
{
    Network = 0,
    Billing = 1,
    SimBlock = 2,
    Provisioning = 3,
}

public enum TechnicalTicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
}

public enum TechnicalTicketStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Escalated = 3,
}
