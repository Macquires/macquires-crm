using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Queries;

public record TechnicalTicketListItemDto(
    string Id,
    string TicketNumber,
    string Msisdn,
    string? CustomerDisplayName,
    TechnicalTicketIssueType IssueType,
    TechnicalTicketCategory TicketCategory,
    TechnicalTicketPriority Priority,
    TechnicalTicketStatus Status,
    string? Notes,
    string? ResolutionNotes,
    DateTime? CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    string OpenedByUserId,
    string CreatedByChannel);

public class GetTechnicalTicketsResult
{
    public List<TechnicalTicketListItemDto>? Data { get; init; }
    public int ActiveOpenCount { get; init; }
}

public class GetTechnicalTicketsRequest : IRequest<GetTechnicalTicketsResult>, IRequirePermission
{
    /// <summary>Dashboard quick queue: Open + InProgress only.</summary>
    public bool ActiveQueueOnly { get; init; }

    public TechnicalTicketStatus? Status { get; init; }
    public string? Msisdn { get; init; }
    public string? CustomerId { get; init; }
    public BackOfficeDomain? Domain { get; init; }

    /// <summary>When false and not ActiveQueueOnly, excludes Resolved only (legacy).</summary>
    public bool IncludeResolved { get; init; }

    public string PermissionKey => PermissionCatalog.NetworkTechnicalView;
}

public class GetTechnicalTicketsHandler : IRequestHandler<GetTechnicalTicketsRequest, GetTechnicalTicketsResult>
{
    private readonly IQueryContext _query;

    public GetTechnicalTicketsHandler(IQueryContext query) => _query = query;

    public async Task<GetTechnicalTicketsResult> Handle(
        GetTechnicalTicketsRequest request,
        CancellationToken cancellationToken)
    {
        var q = _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo();

        if (request.ActiveQueueOnly)
        {
            q = q.Where(t =>
                t.Status == TechnicalTicketStatus.Open
                || t.Status == TechnicalTicketStatus.InProgress);
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

        var activeCount = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(
                t => t.Status == TechnicalTicketStatus.Open || t.Status == TechnicalTicketStatus.InProgress,
                cancellationToken);

        var data = await (
            from t in q
            join c in _query.Customer.AsNoTracking().IsDeletedEqualTo()
                on t.CustomerId equals c.Id into customers
            from c in customers.DefaultIfEmpty()
            orderby t.CreatedAtUtc descending, t.Priority descending
            select new TechnicalTicketListItemDto(
                t.Id,
                t.TicketNumber,
                t.Msisdn,
                c != null ? c.DisplayName : null,
                t.IssueType,
                t.TicketCategory,
                t.Priority,
                t.Status,
                t.Notes,
                t.ResolutionNotes,
                t.CreatedAtUtc,
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
