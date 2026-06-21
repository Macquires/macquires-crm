using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Telecom;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Queries;

public sealed class LiveNetworkStatusDto
{
    public string Msisdn { get; init; } = "";
    public string? SubscriberProfileId { get; init; }
    public string? ProductOfferingName { get; init; }
    public string? CrmOperationalStatus { get; init; }
    public decimal CbsBalance { get; init; }
    public string CbsBalanceLabel { get; init; } = "";
    public bool HlrSuccess { get; init; }
    public string HlrMessage { get; init; } = "";
    public bool HlrIsOnline { get; init; }
    public string? HlrSubscriberState { get; init; }
    public string? HlrLocation { get; init; }
    public bool DiffersFromCrm { get; init; }
    public DateTime QueriedAtUtc { get; init; }
}

public class QueryLiveNetworkStatusByTicketResult
{
    public LiveNetworkStatusDto? Data { get; init; }
}

public class QueryLiveNetworkStatusByTicketRequest : IRequest<QueryLiveNetworkStatusByTicketResult>, IRequireAnyPermission
{
    public string TicketId { get; init; } = "";

    public IReadOnlyList<string> PermissionKeys => BackOfficePermissionSets.TechnicalViewAny;
}

public class QueryLiveNetworkStatusByTicketValidator : AbstractValidator<QueryLiveNetworkStatusByTicketRequest>
{
    public QueryLiveNetworkStatusByTicketValidator() => RuleFor(x => x.TicketId).NotEmpty();
}

public class QueryLiveNetworkStatusByTicketHandler
    : IRequestHandler<QueryLiveNetworkStatusByTicketRequest, QueryLiveNetworkStatusByTicketResult>
{
    private readonly IQueryContext _query;
    private readonly IHLRLiveStatusService _hlr;
    private readonly IBillingSystemIntegration _billing;

    public QueryLiveNetworkStatusByTicketHandler(
        IQueryContext query,
        IHLRLiveStatusService hlr,
        IBillingSystemIntegration billing)
    {
        _query = query;
        _hlr = hlr;
        _billing = billing;
    }

    public async Task<QueryLiveNetworkStatusByTicketResult> Handle(
        QueryLiveNetworkStatusByTicketRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .Where(t => t.Id == request.TicketId)
            .Select(t => new { t.Msisdn, t.SubscriberProfileId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("التذكرة غير موجودة.");

        var line = await TechnicalTicketLineResolver.ResolveForBackOfficeTicketAsync(
            _query,
            ticket.SubscriberProfileId,
            ticket.Msisdn,
            cancellationToken);

        var msisdn = line.Msisdn;
        var profileId = line.SubscriberProfileId;

        string? crmStatus = null;
        string? offeringName = null;
        if (!string.IsNullOrEmpty(profileId))
        {
            var profileRow = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                .Where(p => p.Id == profileId)
                .Select(p => new { p.OperationalStatus })
                .FirstOrDefaultAsync(cancellationToken);

            crmStatus = profileRow?.OperationalStatus.ToString();

            var productId = line.ProductId;
            if (string.IsNullOrEmpty(productId))
            {
                productId = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                    .Where(s => s.SubscriberProfileId == profileId)
                    .OrderByDescending(s => s.CreatedAtUtc)
                    .Select(s => s.ProductId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!string.IsNullOrEmpty(productId))
            {
                offeringName = await _query.ProductOffering.AsNoTracking().IsDeletedEqualTo()
                    .Where(o => o.ProductId == productId)
                    .OrderBy(o => o.SortOrder)
                    .Select(o => o.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                if (string.IsNullOrEmpty(offeringName))
                {
                    offeringName = await _query.Product.AsNoTracking().IsDeletedEqualTo()
                        .Where(p => p.Id == productId)
                        .Select(p => p.Name)
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }
        }

        var hlr = await _hlr.QueryLiveStatusAsync(msisdn, crmStatus, cancellationToken);
        var balance = await _billing.GetOutstandingBalanceAsync(msisdn, cancellationToken);
        var balanceLabel = balance < 0
            ? $"دين معلّق: {Math.Abs(balance):N0} ل.س"
            : $"رصيد: {balance:N0} ل.س";

        return new QueryLiveNetworkStatusByTicketResult
        {
            Data = new LiveNetworkStatusDto
            {
                Msisdn = msisdn,
                SubscriberProfileId = profileId,
                ProductOfferingName = offeringName,
                CrmOperationalStatus = crmStatus,
                CbsBalance = balance,
                CbsBalanceLabel = balanceLabel,
                HlrSuccess = hlr.Success,
                HlrMessage = hlr.Message,
                HlrIsOnline = hlr.IsOnline,
                HlrSubscriberState = hlr.HlrSubscriberState,
                HlrLocation = hlr.Location,
                DiffersFromCrm = hlr.DiffersFromCrm,
                QueriedAtUtc = DateTime.UtcNow,
            },
        };
    }
}
