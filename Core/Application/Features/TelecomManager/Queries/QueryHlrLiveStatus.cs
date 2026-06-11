using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Telecom;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class QueryHlrLiveStatusRequest : IRequest<HlrLiveStatusResult>
{
    public string SubscriberProfileId { get; init; } = "";
    public string? Msisdn { get; init; }
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
        Domain.Entities.SubscriberProfile? profile = null;
        var msisdnQuery = string.IsNullOrWhiteSpace(request.Msisdn)
            ? null
            : Application.Common.Telecom.TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.Msisdn);

        if (!string.IsNullOrWhiteSpace(request.SubscriberProfileId))
        {
            profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                .FirstOrDefaultAsync(p => p.Id == request.SubscriberProfileId, cancellationToken)
                ?? throw new InvalidOperationException("Subscriber profile not found.");
        }
        else if (!string.IsNullOrEmpty(msisdnQuery))
        {
            profile = await (
                from p in _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                join s in _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo() on p.Id equals s.SubscriberProfileId
                join m in _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo() on s.MsisdnAssetId equals m.Id
                where m.Msisdn == msisdnQuery
                select p)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Subscriber profile not found for MSISDN.");
        }
        else
        {
            throw new InvalidOperationException("SubscriberProfileId or MSISDN is required.");
        }

        string? msisdn = msisdnQuery;
        if (string.IsNullOrEmpty(msisdn))
        {
            try
            {
                var line = await SubscriberLineResolver.ResolveAssetAsync(
                    _query,
                    profile.Id,
                    msisdnAssetId: null,
                    msisdn: null,
                    cancellationToken);
                msisdn = line.Msisdn;
            }
            catch (InvalidOperationException)
            {
                msisdn = null;
            }
        }

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
