using Domain.Entities;
using Domain.Enums;

namespace Domain.Services;

public sealed record BindSubscriptionCommand(
    string CustomerId,
    string SubscriberProfileId,
    string MsisdnAssetId,
    string SimInventoryId,
    string ProductOfferingId,
    string TelecomOperationRequestId,
    string? CorrelationId = null,
    TelecomDocumentStatus DocumentStatus = TelecomDocumentStatus.Missing);

public sealed class SubscriptionBindingContext
{
    public required Customer Customer { get; init; }
    public required MsisdnAsset MsisdnAsset { get; init; }
    public required SimInventory SimInventory { get; init; }
    public required SubscriberProfile SubscriberProfile { get; init; }
    public int ActiveLineCountForCustomer { get; init; }
    public int MaxLinesAllowed { get; init; } = 10;
    public bool CustomerHasPrimaryLine { get; init; }
    public DateTime UtcNow { get; init; } = DateTime.UtcNow;

    /// <summary>When true, MSISDN must be Reserved for the same customer (enterprise activation path).</summary>
    public bool RequireStrictReservation { get; init; }
}

public sealed record BindSubscriptionResult(
    SubscriberProfile SubscriberProfile,
    TelecomSubscription TelecomSubscription,
    bool IsPrimaryLine,
    string Msisdn,
    string Iccid);

public interface ISubscriptionBindingService
{
    BindSubscriptionResult Bind(BindSubscriptionCommand command, SubscriptionBindingContext context);
}

public interface ICustomerLineLimitPolicy
{
    int GetMaxLinesAllowed(Customer customer, int currentActiveLineCount);
}
