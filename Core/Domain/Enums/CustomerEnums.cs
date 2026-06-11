namespace Domain.Enums;

public enum CustomerStatus
{
    Active = 0,
    Suspended = 1,
    Closed = 2,
    Blacklisted = 3
}

public enum CustomerStatusReasonCode
{
    None = 0,
    Credit = 1,
    Fraud = 2,
    Regulatory = 3
}

public enum BillingConsolidationMode
{
    Separate = 0,
    Unified = 1
}

public enum LanguagePreference
{
    Arabic = 0,
    English = 1
}

public enum CustomerKind
{
    Individual = 0,
    Corporate = 1
}

public enum CompanyLegalStatus
{
    Unknown = 0,
    SoleProprietorship = 1,
    Partnership = 2,
    LimitedLiability = 3,
    JointStock = 4,
    Government = 5
}

public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2
}

public enum ServiceLineType
{
    Mobile = 0,
    Broadband = 1,
    FixedLine = 2
}

public enum SubscriberOperationalStatus
{
    Pending = 0,
    Active = 1,
    Suspended = 2,
    Terminated = 3,
    SuspendedInbound = 4,
    SuspendedOutbound = 5,
    Deactivated = 6
}

public enum MsisdnCategory
{
    Normal = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3
}

public enum SimStatus
{
    Available = 0,
    Reserved = 1,
    Active = 2,
    Suspended = 3,
    Quarantined = 4,
    /// <summary>Permanently dead after recycling / force unpair — never re-issued.</summary>
    Burned = 5
}

public enum SimType
{
    Physical = 0,
    ESim = 1
}
