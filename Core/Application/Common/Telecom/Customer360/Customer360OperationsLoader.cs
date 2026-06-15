using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Telecom;
using Application.Features.CustomerManager.Queries;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Customer360;

public interface ICustomer360OperationsLoader
{
    Task<IReadOnlyList<Customer360OperationDto>> LoadRecentAsync(
        IReadOnlyList<string> profileIds,
        CancellationToken cancellationToken);
}

public sealed class Customer360OperationsLoader : ICustomer360OperationsLoader
{
    private readonly IQueryContext _query;

    public Customer360OperationsLoader(IQueryContext query)
    {
        _query = query;
    }

    public async Task<IReadOnlyList<Customer360OperationDto>> LoadRecentAsync(
        IReadOnlyList<string> profileIds,
        CancellationToken cancellationToken)
    {
        if (profileIds.Count == 0)
        {
            return [];
        }

        var operationRows = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => profileIds.Contains(o.SubscriberProfileId)
                        || (o.SecondarySubscriberProfileId != null
                            && profileIds.Contains(o.SecondarySubscriberProfileId)))
            .Include(o => o.MsisdnAsset)
            .Include(o => o.SubscriberProfile!).ThenInclude(p => p!.Customer)
            .Include(o => o.SecondarySubscriberProfile!).ThenInclude(p => p!.Customer)
            .AsSplitQuery()
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(15)
            .ToListAsync(cancellationToken);

        return operationRows.Select(o =>
        {
            var isIncomingTakeOver = o.Kind == TelecomOperationKind.TakeOver
                && o.SecondarySubscriberProfileId != null
                && profileIds.Contains(o.SecondarySubscriberProfileId);
            var counterparty = isIncomingTakeOver
                ? o.SubscriberProfile?.Customer?.DisplayName
                : o.SecondarySubscriberProfile?.Customer?.DisplayName;

            return new Customer360OperationDto(
                o.Id,
                o.Number,
                o.Kind,
                TelecomOperationLabels.KindLabelAr(o.Kind),
                o.Status,
                TelecomOperationLabels.StatusLabelAr(o.Status),
                o.TransferReason,
                o.MsisdnAsset?.Msisdn,
                counterparty,
                o.CorrelationId,
                o.CreatedAtUtc,
                TelecomOperationSchedulePolicy.ResolveEffectiveDateUtc(o));
        }).ToList();
    }
}
