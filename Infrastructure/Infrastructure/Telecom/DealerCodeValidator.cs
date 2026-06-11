using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Telecom.SellingLine;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telecom;

public sealed class DealerCodeValidator : IDealerCodeValidator
{
    private readonly IQueryContext _query;

    public DealerCodeValidator(IQueryContext query) => _query = query;

    public async Task<bool> ExistsAsync(string dealerCode, CancellationToken cancellationToken)
    {
        var code = (dealerCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        return await _query.Customer.AsNoTracking()
            .IsDeletedEqualTo(false)
            .OfType<CorporateCustomer>()
            .AnyAsync(c => c.AccountNumber == code, cancellationToken);
    }
}
