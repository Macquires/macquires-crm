namespace Application.Common.Telecom.OperationConfirm;

public sealed record OperationProvisionContext(
    string? Msisdn,
    string? Iccid,
    string? CustomerId,
    string? SubscriberProfileId,
    string? TelecomSubscriptionId,
    string? MsisdnAssetId,
    string? SimInventoryId);
