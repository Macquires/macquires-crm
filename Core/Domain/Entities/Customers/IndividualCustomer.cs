using Domain.Enums;

using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>عميل فرد — الرقم الوطني فريد على مستوى النظام.</summary>
public sealed class IndividualCustomer : Customer
{
    public string NationalId { get; private set; } = null!;
    /// <summary>HMAC for equality search when NationalId is encrypted at rest.</summary>
    public string? NationalIdSearchHash { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Nationality { get; private set; }
    public Gender Gender { get; private set; } = Gender.Unknown;
    public string? Occupation { get; private set; }

    private IndividualCustomer() { }

    public static IndividualCustomer Create(
        string displayName,
        string accountNumber,
        string nationalId,
        PostalAddress address,
        string? contactEmail,
        string? primaryPhone,
        string? customerGroupId,
        string? customerCategoryId,
        DateOnly? dateOfBirth = null,
        string? nationality = null,
        Gender gender = Gender.Unknown,
        string? occupation = null,
        string? description = null)
    {
        return new IndividualCustomer
        {
            CustomerKind = CustomerKind.Individual,
            DisplayName = displayName,
            AccountNumber = accountNumber,
            NationalId = nationalId,
            Address = address,
            ContactEmail = contactEmail,
            PrimaryPhone = primaryPhone,
            CustomerGroupId = customerGroupId,
            CustomerCategoryId = customerCategoryId,
            DateOfBirth = dateOfBirth,
            Nationality = nationality,
            Gender = gender,
            Occupation = occupation,
            Description = description
        };
    }

    public void UpdateIdentity(string nationalId, DateOnly? dateOfBirth, string? nationality, Gender gender, string? occupation)
    {
        NationalId = nationalId;
        DateOfBirth = dateOfBirth;
        Nationality = nationality;
        Gender = gender;
        Occupation = occupation;
    }

    public void SetNationalIdSearchHash(string? hash) => NationalIdSearchHash = hash;
}
