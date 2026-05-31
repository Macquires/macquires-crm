namespace Domain.Common;

/// <summary>Stable primary keys for seeded subscription line types (must match SQL patch seed rows).</summary>
public static class TelecomSubscriptionTypeWellKnownIds
{
    public const string Prepaid = "a0e0e0e0-0000-4000-8000-000000000001";
    public const string Postpaid = "a0e0e0e0-0000-4000-8000-000000000002";
    public const string Hybrid = "a0e0e0e0-0000-4000-8000-000000000003";
}
