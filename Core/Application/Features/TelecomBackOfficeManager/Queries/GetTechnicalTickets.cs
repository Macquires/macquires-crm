using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Queries;

public record TechnicalTicketListItemDto(
    string Id,
    string TicketNumber,
    string? CustomerId,
    string Msisdn,
    string? CustomerDisplayName,
    TechnicalTicketIssueType IssueType,
    TechnicalTicketCategory TicketCategory,
    TechnicalTicketPriority Priority,
    TechnicalTicketStatus Status,
    string? Notes,
    string? ResolutionNotes,
    DateTime? CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ResolvedAtUtc,
    string OpenedByUserId,
    string CreatedByChannel);

public class GetTechnicalTicketsResult
{
    public List<TechnicalTicketListItemDto>? Data { get; init; }
    public int ActiveOpenCount { get; init; }
}

public class GetTechnicalTicketsRequest : IRequest<GetTechnicalTicketsResult>, IRequireAnyPermission
{
    /// <summary>Dashboard quick queue: Open + InProgress + Escalated (Tier-3).</summary>
    public bool ActiveQueueOnly { get; init; }

    public TechnicalTicketStatus? Status { get; init; }
    public string? Msisdn { get; init; }
    public string? CustomerId { get; init; }
    public BackOfficeDomain? Domain { get; init; }

    /// <summary>When false and not ActiveQueueOnly, excludes Resolved only (legacy).</summary>
    public bool IncludeResolved { get; init; }

    /// <summary>Call-center landing: tickets opened by the signed-in agent only.</summary>
    public bool OpenedByCurrentUserOnly { get; init; }

    public IReadOnlyList<string> PermissionKeys => BackOfficePermissionSets.TechnicalTicketListAny;
}

public class GetTechnicalTicketsHandler : IRequestHandler<GetTechnicalTicketsRequest, GetTechnicalTicketsResult>
{
    private readonly IQueryContext _query;
    private readonly IOperatorContext _operator;

    public GetTechnicalTicketsHandler(IQueryContext query, IOperatorContext @operator)
    {
        _query = query;
        _operator = @operator;
    }

    public async Task<GetTechnicalTicketsResult> Handle(
        GetTechnicalTicketsRequest request,
        CancellationToken cancellationToken)
    {
        var q = _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo();

        if (request.OpenedByCurrentUserOnly)
        {
            var userId = _operator.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new GetTechnicalTicketsResult { Data = [], ActiveOpenCount = 0 };
            }

            q = q.Where(t => t.OpenedByUserId == userId);
            q = q.Where(t =>
                t.CustomerId != null
                && _query.Customer.Any(c => !c.IsDeleted && c.Id == t.CustomerId));
            q = q.Where(t =>
                t.CreatedByChannel == TechnicalTicketCreatedByChannel.CallCenterAgent
                || t.CreatedByChannel == TechnicalTicketCreatedByChannel.CustomerCareVoiceAi
                || t.CreatedByChannel == TechnicalTicketCreatedByChannel.ShowroomAgent);
        }

        var activeQueueOnly = request.ActiveQueueOnly || request.OpenedByCurrentUserOnly;

        if (activeQueueOnly)
        {
            q = q.Where(t =>
                t.Status == TechnicalTicketStatus.Open
                || t.Status == TechnicalTicketStatus.InProgress
                || t.Status == TechnicalTicketStatus.Escalated);
        }
        else if (request.Status.HasValue)
        {
            q = q.Where(t => t.Status == request.Status.Value);
        }
        else if (!request.IncludeResolved)
        {
            q = q.Where(t => t.Status != TechnicalTicketStatus.Resolved);
        }

        if (!string.IsNullOrWhiteSpace(request.Msisdn))
        {
            var msisdn = request.Msisdn.Trim();
            q = q.Where(t => t.Msisdn.Contains(msisdn));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            var customerId = request.CustomerId.Trim();
            q = q.Where(t => t.CustomerId == customerId);
        }

        if (request.Domain.HasValue && request.Domain.Value != BackOfficeDomain.NetworkAndTechnical)
        {
            // Technical tickets are primarily NetworkAndTechnical. 
            // If another domain is requested, we return empty unless we have a mapping for it.
            q = q.Where(t => false);
        }

        var activeCountQuery = _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .Where(t =>
                t.Status == TechnicalTicketStatus.Open
                || t.Status == TechnicalTicketStatus.InProgress
                || t.Status == TechnicalTicketStatus.Escalated);

        if (request.OpenedByCurrentUserOnly && !string.IsNullOrWhiteSpace(_operator.UserId))
        {
            var userId = _operator.UserId!;
            activeCountQuery = activeCountQuery.Where(t => t.OpenedByUserId == userId);
            activeCountQuery = activeCountQuery.Where(t =>
                t.CustomerId != null
                && _query.Customer.Any(c => !c.IsDeleted && c.Id == t.CustomerId));
        }

        var activeCount = await activeCountQuery.CountAsync(cancellationToken);

        var data = await (
            from t in q
            join c in _query.Customer.AsNoTracking().IsDeletedEqualTo()
                on t.CustomerId equals c.Id into customers
            from c in customers.DefaultIfEmpty()
            orderby
                t.Status == TechnicalTicketStatus.Escalated ? 0 : 1,
                (t.UpdatedAtUtc ?? t.CreatedAtUtc) descending,
                t.Priority descending
            select new TechnicalTicketListItemDto(
                t.Id,
                t.TicketNumber,
                t.CustomerId,
                t.Msisdn,
                c != null ? c.DisplayName : null,
                t.IssueType,
                t.TicketCategory,
                t.Priority,
                t.Status,
                t.Notes,
                t.ResolutionNotes,
                t.CreatedAtUtc,
                t.UpdatedAtUtc,
                t.ResolvedAtUtc,
                t.OpenedByUserId,
                t.CreatedByChannel))
            .ToListAsync(cancellationToken);

        return new GetTechnicalTicketsResult
        {
            Data = data,
            ActiveOpenCount = activeCount,
        };
    }
}
