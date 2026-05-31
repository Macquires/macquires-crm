using Domain.Enums;

using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>عميل شركة — السجل التجاري فريد على مستوى النظام.</summary>
public sealed class CorporateCustomer : Customer
{
    public string CommercialRegistryNumber { get; private set; } = null!;
    public string? ParentCustomerId { get; private set; }
    public Customer? ParentCustomer { get; private set; }
    public BillingConsolidationMode BillingConsolidationMode { get; private set; } = BillingConsolidationMode.Separate;
    public string? TaxNumber { get; private set; }
    public string? AuthorizedSignatoryName { get; private set; }
    public CompanyLegalStatus LegalStatus { get; private set; } = CompanyLegalStatus.Unknown;

    private CorporateCustomer() { }

    public static CorporateCustomer Create(
        string displayName,
        string accountNumber,
        string commercialRegistryNumber,
        PostalAddress address,
        string? contactEmail,
        string? primaryPhone,
        string? customerGroupId,
        string? customerCategoryId,
        string? taxNumber = null,
        string? authorizedSignatoryName = null,
        CompanyLegalStatus legalStatus = CompanyLegalStatus.Unknown,
        string? description = null,
        string? parentCustomerId = null,
        BillingConsolidationMode billingConsolidationMode = BillingConsolidationMode.Separate)
    {
        return new CorporateCustomer
        {
            CustomerKind = CustomerKind.Corporate,
            DisplayName = displayName,
            AccountNumber = accountNumber,
            CommercialRegistryNumber = commercialRegistryNumber,
            Address = address,
            ContactEmail = contactEmail,
            PrimaryPhone = primaryPhone,
            CustomerGroupId = customerGroupId,
            CustomerCategoryId = customerCategoryId,
            TaxNumber = taxNumber,
            AuthorizedSignatoryName = authorizedSignatoryName,
            LegalStatus = legalStatus,
            Description = description,
            ParentCustomerId = parentCustomerId,
            BillingConsolidationMode = billingConsolidationMode
        };
    }

    public void SetCorporateHierarchy(string? parentCustomerId, BillingConsolidationMode mode)
    {
        ParentCustomerId = parentCustomerId;
        BillingConsolidationMode = mode;
    }

    public void UpdateCorporateIdentity(
        string commercialRegistryNumber,
        string? taxNumber,
        string? authorizedSignatoryName,
        CompanyLegalStatus legalStatus)
    {
        CommercialRegistryNumber = commercialRegistryNumber;
        TaxNumber = taxNumber;
        AuthorizedSignatoryName = authorizedSignatoryName;
        LegalStatus = legalStatus;
    }
}
