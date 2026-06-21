using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom.Customer360;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public enum Customer360TimelineKind
{
    Operation = 0,
    Payment = 1,
    Ticket = 2,
    Billing = 3,
    Audit = 4,
}

public sealed class Customer360TimelineItemDto
{
    public DateTime OccurredAtUtc { get; init; }
    public Customer360TimelineKind Kind { get; init; }
    public string TitleAr { get; init; } = null!;
    public string TitleEn { get; init; } = null!;
    public string? Subtitle { get; init; }
    public string? Status { get; init; }
    public string? ReferenceId { get; init; }
    public string? ActionUrl { get; init; }
}

public class GetCustomer360TimelineResult
{
    public IReadOnlyList<Customer360TimelineItemDto> Items { get; init; } = [];
    public bool HasMore { get; init; }
    public int TotalAvailable { get; init; }
}

public class GetCustomer360TimelineRequest : IRequest<GetCustomer360TimelineResult>, IRequirePermission
{
    public string CustomerId { get; init; } = "";
    public int Take { get; init; } = 50;
    public int Skip { get; init; }
    /// <summary>Comma-separated <see cref="Customer360TimelineKind"/> values.</summary>
    public string? Kinds { get; init; }
    public string PermissionKey => PermissionCatalog.CustomerView;
}

public class GetCustomer360TimelineHandler : IRequestHandler<GetCustomer360TimelineRequest, GetCustomer360TimelineResult>
{
    private readonly IQueryContext _query;
    private readonly IUserAuditReadService _auditRead;

    public GetCustomer360TimelineHandler(IQueryContext query, IUserAuditReadService auditRead)
    {
        _query = query;
        _auditRead = auditRead;
    }

    public async Task<GetCustomer360TimelineResult> Handle(
        GetCustomer360TimelineRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = request.CustomerId.Trim();
        if (string.IsNullOrEmpty(customerId))
        {
            return new GetCustomer360TimelineResult();
        }

        var take = Math.Clamp(request.Take, 1, 100);
        var skip = Math.Max(0, request.Skip);
        var fetchCap = Math.Min(skip + take + 1, 250);
        var kindFilter = ParseKinds(request.Kinds);

        var profileIds = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == customerId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var items = new List<Customer360TimelineItemDto>();

        if (profileIds.Count > 0)
        {
            var ops = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
                .Where(o => profileIds.Contains(o.SubscriberProfileId))
                .OrderByDescending(o => o.CreatedAtUtc)
                .Take(fetchCap)
                .Select(o => new
                {
                    o.Id,
                    o.Number,
                    o.Kind,
                    o.Status,
                    o.CreatedAtUtc,
                    Msisdn = o.MsisdnAsset != null ? o.MsisdnAsset.Msisdn : null,
                })
                .ToListAsync(cancellationToken);

            items.AddRange(ops.Select(o => Customer360TimelineComposer.MapOperation(
                o.Kind,
                o.Status,
                o.Number,
                o.Msisdn,
                o.CreatedAtUtc,
                o.Id)));
        }

        var payments = await _query.TelecomPaymentTransaction.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(fetchCap)
            .Select(p => new
            {
                p.Number,
                p.Amount,
                p.Status,
                p.TransactionType,
                p.Msisdn,
                p.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        items.AddRange(payments.Select(p => Customer360TimelineComposer.MapPayment(
            p.TransactionType,
            p.Status,
            p.Amount,
            p.Msisdn,
            p.Number,
            p.CreatedAtUtc)));

        var tickets = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(fetchCap)
            .Select(t => new
            {
                t.TicketNumber,
                t.Status,
                t.Priority,
                t.IssueType,
                t.Msisdn,
                t.Notes,
                t.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        items.AddRange(tickets.Select(t => Customer360TimelineComposer.MapTicket(
            t.TicketNumber,
            t.Status,
            t.Priority,
            t.IssueType,
            t.Msisdn,
            t.Notes,
            t.CreatedAtUtc)));

        if (profileIds.Count > 0)
        {
            var billing = await GetCustomer360SupplementsHandler.LoadBillingLogsAsync(
                profileIds,
                cancellationToken,
                _query);

            items.AddRange(billing.Select(Customer360TimelineComposer.MapBilling));
        }

        var audit = await _auditRead.QueryAsync(
            new UserAuditLogQuery { CustomerId = customerId, Skip = 0, Take = fetchCap },
            cancellationToken);

        items.AddRange(audit.Items
            .Where(a => Customer360TimelineComposer.ShouldIncludeAudit(a.ActionType))
            .Select(Customer360TimelineComposer.MapAudit));

        var merged = items
            .Where(i => i.OccurredAtUtc != DateTime.MinValue)
            .Where(i => kindFilter.Count == 0 || kindFilter.Contains(i.Kind))
            .OrderByDescending(i => i.OccurredAtUtc)
            .ToList();

        var page = merged.Skip(skip).Take(take).ToList();
        return new GetCustomer360TimelineResult
        {
            Items = page,
            HasMore = merged.Count > skip + take,
            TotalAvailable = merged.Count,
        };
    }

    private static HashSet<Customer360TimelineKind> ParseKinds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var set = new HashSet<Customer360TimelineKind>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<Customer360TimelineKind>(part, true, out var kind))
            {
                set.Add(kind);
            }
            else if (int.TryParse(part, out var n) && Enum.IsDefined(typeof(Customer360TimelineKind), n))
            {
                set.Add((Customer360TimelineKind)n);
            }
        }

        return set;
    }
}
