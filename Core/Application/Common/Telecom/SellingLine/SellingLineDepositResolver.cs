using Application.Common.CQS.Queries;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.SellingLine;

public static class SellingLineDepositResolver
{
    public static async Task<decimal> ResolveRequiredDepositAsync(
        IQueryContext query,
        TelecomOperationRequest entity,
        CancellationToken cancellationToken)
    {
        if (entity.InitialDepositAmount is > 0)
        {
            return entity.InitialDepositAmount.Value;
        }

        if (string.IsNullOrEmpty(entity.ProductId))
        {
            return 0m;
        }

        var unitPrice = await query.Product.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == entity.ProductId)
            .Select(p => p.UnitPrice)
            .FirstOrDefaultAsync(cancellationToken);

        return unitPrice is > 0 ? (decimal)unitPrice : 0m;
    }
}
