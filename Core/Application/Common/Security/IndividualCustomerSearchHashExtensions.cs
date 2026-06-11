using Domain.Entities;

namespace Application.Common.Security;

public static class IndividualCustomerSearchHashExtensions
{
    public static void SyncNationalIdSearchHash(this IndividualCustomer customer, IFieldEncryptionService encryption)
    {
        var nationalId = customer.NationalId?.Trim();
        if (string.IsNullOrEmpty(nationalId))
        {
            return;
        }

        customer.SetNationalIdSearchHash(encryption.ComputeSearchHash(nationalId));
    }
}
