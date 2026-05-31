namespace Domain.Enums;

/// <summary>Type of service component inside a <see cref="Entities.ProductOfferingComponent"/>.</summary>
public enum ServiceComponentType
{
    Voice = 0,
    Data = 1,
    Sms = 2,
    /// <summary>Value-Added Services (caller tune, insurance, etc.).</summary>
    Vas = 3,
    /// <summary>Physical equipment bundled with the offering (router, handset).</summary>
    Equipment = 4,
    /// <summary>International voice/data minutes.</summary>
    International = 5
}

/// <summary>Pricing model for a <see cref="Entities.PricePlan"/>.</summary>
public enum PricePlanType
{
    Monthly = 0,
    Daily = 1,
    Weekly = 2,
    PayAsYouGo = 3,
    /// <summary>One-time activation or installation fee.</summary>
    OneTime = 4
}

public enum PaymentType
{
    Prepaid = 0,
    Postpaid = 1,
    Hybrid = 2
}

public enum BillingCycle
{
    Monthly = 0,
    Weekly = 1,
    Daily = 2,
    OneTime = 3
}

public enum IdentityDocumentType
{
    NationalId = 0,
    Passport = 1,
    ResidencePermit = 2,
    CommercialRegistry = 3,
    Other = 99
}
