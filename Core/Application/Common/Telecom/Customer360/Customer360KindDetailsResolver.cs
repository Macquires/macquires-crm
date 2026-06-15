using Application.Common.CQS.Queries;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Customer360;

public sealed record Customer360KindDetails(
    string? NationalIdMasked,
    string? CommercialRegistry,
    DateOnly? DateOfBirth,
    string? Nationality,
    Gender? Gender,
    string? GenderLabel,
    string? Occupation,
    string? TaxNumber,
    string? AuthorizedSignatoryName,
    CompanyLegalStatus? LegalStatus,
    string? LegalStatusLabel,
    BillingConsolidationMode? BillingConsolidationMode,
    string? BillingConsolidationModeLabel,
    string? ParentCustomerDisplayName);

public interface ICustomer360KindDetailsResolver
{
    Task<Customer360KindDetails> ResolveAsync(Customer customer, CancellationToken cancellationToken);
}

public sealed class Customer360KindDetailsResolver : ICustomer360KindDetailsResolver
{
    private readonly IQueryContext _query;

    public Customer360KindDetailsResolver(IQueryContext query)
    {
        _query = query;
    }

    public async Task<Customer360KindDetails> ResolveAsync(Customer customer, CancellationToken cancellationToken)
    {
        if (customer is IndividualCustomer ind)
        {
            var decrypted = ind.NationalId;
            var nationalMasked = decrypted.Length >= 4
                ? new string('*', decrypted.Length - 4) + decrypted[^4..]
                : "****";

            return new Customer360KindDetails(
                nationalMasked,
                null,
                ind.DateOfBirth,
                ind.Nationality,
                ind.Gender,
                ind.Gender switch
                {
                    Gender.Male => "ذكر",
                    Gender.Female => "أنثى",
                    _ => "غير محدد",
                },
                ind.Occupation,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        if (customer is CorporateCustomer corp)
        {
            string? parentCustomerName = null;
            if (!string.IsNullOrEmpty(corp.ParentCustomerId))
            {
                parentCustomerName = await _query.Customer.AsNoTracking()
                    .Where(c => c.Id == corp.ParentCustomerId)
                    .Select(c => c.DisplayName)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return new Customer360KindDetails(
                null,
                corp.CommercialRegistryNumber,
                null,
                null,
                null,
                null,
                null,
                corp.TaxNumber,
                corp.AuthorizedSignatoryName,
                corp.LegalStatus,
                corp.LegalStatus switch
                {
                    CompanyLegalStatus.SoleProprietorship => "مؤسسة فردية",
                    CompanyLegalStatus.Partnership => "شراكة",
                    CompanyLegalStatus.LimitedLiability => "ذات مسؤولية محدودة",
                    CompanyLegalStatus.JointStock => "مساهمة",
                    CompanyLegalStatus.Government => "حكومية",
                    _ => "غير محدد",
                },
                corp.BillingConsolidationMode,
                corp.BillingConsolidationMode switch
                {
                    BillingConsolidationMode.Unified => "موحّد",
                    _ => "منفصل",
                },
                parentCustomerName);
        }

        return new Customer360KindDetails(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null);
    }
}
