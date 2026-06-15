using Domain.Enums;

namespace Application.Features.CustomerManager.Queries;

public record Customer360OperationDto(
    string Id,
    string Number,
    TelecomOperationKind Kind,
    string? KindLabelAr,
    TelecomOperationStatus Status,
    string? StatusLabelAr,
    string? TransferReason,
    string? Msisdn,
    string? CounterpartyNameAr,
    string? CorrelationId,
    DateTime? CreatedAtUtc,
    DateTime? ScheduledEffectiveDateUtc);

public record Customer360ContactDto(
    string Id,
    string? Name,
    string? JobTitle,
    string? PhoneNumber,
    string? EmailAddress,
    string? Description);

public record Customer360PackageComponentDto(
    ServiceComponentType ComponentType,
    string? Label,
    decimal? Quota,
    string? QuotaUnit,
    bool IsUnlimited,
    int SortOrder);

public record Customer360SubscriptionDto(
    string Id,
    string SubscriberProfileId,
    string? Msisdn,
    string? MsisdnAssetId,
    bool IsPrimaryLine,
    string? ProductId,
    string? ProductName,
    string? ProductOfferingId,
    string? ProductOfferingName,
    string? ProductOfferingNameEn,
    string? SubscriptionTypeName,
    string? SubscriptionTypeNameEn,
    string? SubscriptionTypeCode,
    string? SimType,
    string? Iccid,
    string? ProfileOperationalStatus,
    string? DocumentStatusLabel,
    string? ServiceLineTypeLabel,
    string? LanguagePreferenceLabel,
    DateTime? ActivationDateUtc,
    int LoyaltyPoints,
    string? LoyaltyTier,
    decimal? PrepaidBalance,
    decimal? PostpaidCreditLimit,
    int? ChurnRiskScore,
    string? SimStatus,
    string? Imsi,
    DateTime? CreatedAtUtc,
    string? DocumentOperationId,
    /// <summary>identity = operation document store; kyc = sovereign KYC vault.</summary>
    string? DocumentSource,
    List<Customer360PackageComponentDto> PackageComponents);

public class GetCustomer360Result
{
    public string CustomerId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string AccountNumber { get; init; } = "";
    public string? Description { get; init; }
    public CustomerKind CustomerKind { get; init; }
    public string CustomerKindLabel { get; init; } = "";
    public CustomerStatus Status { get; init; }
    public string StatusLabel { get; init; } = "";
    public string? StatusReasonCodeLabel { get; init; }
    public string? StatusReasonNote { get; init; }
    public string? NationalIdMasked { get; init; }
    public string? CommercialRegistry { get; init; }
    public string? PrimaryPhone { get; init; }
    public string? ContactEmail { get; init; }
    public string? FaxNumber { get; init; }
    public string? Website { get; init; }
    public string? WhatsApp { get; init; }
    public string? LinkedIn { get; init; }
    public string? Facebook { get; init; }
    public string? Instagram { get; init; }
    public string? TwitterX { get; init; }
    public string? TikTok { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public string? CustomerGroupName { get; init; }
    public string? CustomerCategoryName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Nationality { get; init; }
    public Gender? Gender { get; init; }
    public string? GenderLabel { get; init; }
    public CompanyLegalStatus? LegalStatus { get; init; }
    public BillingConsolidationMode? BillingConsolidationMode { get; init; }
    public string? Occupation { get; init; }
    public string? TaxNumber { get; init; }
    public string? AuthorizedSignatoryName { get; init; }
    public string? LegalStatusLabel { get; init; }
    public string? BillingConsolidationModeLabel { get; init; }
    public string? ParentCustomerDisplayName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public List<Customer360ContactDto> Contacts { get; init; } = new();
    public List<Customer360SubscriptionDto> ActiveSubscriptions { get; init; } = new();
    public List<Customer360OperationDto> RecentOperations { get; init; } = new();
}
