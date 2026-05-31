using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class QueryHlrLiveStatusRequest : IRequest<HlrLiveStatusResult>
{
    public string SubscriberProfileId { get; init; } = "";
}

public class QueryHlrLiveStatusHandler : IRequestHandler<QueryHlrLiveStatusRequest, HlrLiveStatusResult>
{
    private readonly IQueryContext _query;
    private readonly IHLRLiveStatusService _hlr;

    public QueryHlrLiveStatusHandler(IQueryContext query, IHLRLiveStatusService hlr)
    {
        _query = query;
        _hlr = hlr;
    }

    public async Task<HlrLiveStatusResult> Handle(QueryHlrLiveStatusRequest request, CancellationToken cancellationToken)
    {
        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(p => p.Id == request.SubscriberProfileId, cancellationToken)
            ?? throw new InvalidOperationException("Subscriber profile not found.");

        var msisdn = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfileId == profile.Id)
            .Select(s => s.MsisdnAsset!.Msisdn)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(msisdn))
        {
            return new HlrLiveStatusResult(false, "لا يوجد MSISDN نشط.", false, null, null, null, false, null);
        }

        return await _hlr.QueryLiveStatusAsync(
            msisdn,
            profile.OperationalStatus.ToString(),
            cancellationToken);
    }
}
