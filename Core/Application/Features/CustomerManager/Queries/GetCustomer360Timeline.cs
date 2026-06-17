using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
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

            items.AddRange(ops.Select(o => new Customer360TimelineItemDto
            {
                OccurredAtUtc = o.CreatedAtUtc ?? DateTime.MinValue,
                Kind = Customer360TimelineKind.Operation,
                TitleAr = $"عملية {o.Kind}",
                TitleEn = $"Operation {o.Kind}",
                Subtitle = o.Msisdn,
                Status = o.Status.ToString(),
                ReferenceId = o.Number,
                ActionUrl = $"/Telecom/TelecomHub?operationId={o.Id}",
            }));
        }

        var payments = await _query.TelecomPaymentTransaction.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(fetchCap)
            .Select(p => new
            {
                p.Id,
                p.Number,
                p.Amount,
                p.Status,
                p.TransactionType,
                p.Msisdn,
                p.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        items.AddRange(payments.Select(p => new Customer360TimelineItemDto
        {
            OccurredAtUtc = p.CreatedAtUtc ?? DateTime.MinValue,
            Kind = Customer360TimelineKind.Payment,
            TitleAr = $"دفع {p.TransactionType}",
            TitleEn = $"Payment {p.TransactionType}",
            Subtitle = $"{p.Amount:N0} ل.س — {p.Msisdn}",
            Status = p.Status.ToString(),
            ReferenceId = p.Number,
        }));

        var tickets = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(fetchCap)
            .Select(t => new
            {
                t.Id,
                t.TicketNumber,
                t.Status,
                t.Priority,
                t.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        items.AddRange(tickets.Select(t => new Customer360TimelineItemDto
        {
            OccurredAtUtc = t.CreatedAtUtc ?? DateTime.MinValue,
            Kind = Customer360TimelineKind.Ticket,
            TitleAr = "تذكرة دعم فني",
            TitleEn = "Technical support ticket",
            Subtitle = t.Priority.ToString(),
            Status = t.Status.ToString(),
            ReferenceId = t.TicketNumber,
            ActionUrl = "/Telecom/TechnicalTicketList",
        }));

        if (profileIds.Count > 0)
        {
            var billing = await GetCustomer360SupplementsHandler.LoadBillingLogsAsync(
                profileIds,
                cancellationToken,
                _query);

            items.AddRange(billing.Select(b => new Customer360TimelineItemDto
            {
                OccurredAtUtc = b.CreatedAtUtc ?? DateTime.MinValue,
                Kind = Customer360TimelineKind.Billing,
                TitleAr = "تكامل فوترة",
                TitleEn = "Billing integration",
                Subtitle = b.IntegrationTarget,
                Status = b.Success ? "نجاح" : "فشل",
                ReferenceId = b.OperationNumber ?? b.IntegrationTarget,
            }));
        }

        var audit = await _auditRead.QueryAsync(
            new UserAuditLogQuery { CustomerId = customerId, Skip = 0, Take = fetchCap },
            cancellationToken);

        items.AddRange(audit.Items.Select(a => new Customer360TimelineItemDto
        {
            OccurredAtUtc = a.OccurredAtUtc,
            Kind = Customer360TimelineKind.Audit,
            TitleAr = a.SummaryAr ?? a.ActionType,
            TitleEn = a.SummaryAr ?? a.ActionType,
            Subtitle = a.ActorDisplayName,
            ReferenceId = a.Id,
        }));

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
