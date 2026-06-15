using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Features.TelecomBackOfficeManager.Queries;
using Application.Features.TelecomManager.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public class GetCustomer360SupplementsResult
{
    public List<Customer360VasServiceDto> ActiveVasServices { get; init; } = new();
    public List<TechnicalTicketListItemDto> SupportTickets { get; init; } = new();
    public List<GetBillingIntegrationLogListDto> BillingLogs { get; init; } = new();
    public List<UserAuditLogListItemDto> ActivityLogs { get; init; } = new();
    public List<TelecomOperationListItemDto> TelecomOperations { get; init; } = new();
}

public class TelecomOperationListItemDto
{
    public string Id { get; init; } = "";
    public string Number { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Status { get; init; } = "";
    public string Msisdn { get; init; } = "";
    public string? MsisdnAssetId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public class GetCustomer360SupplementsRequest : IRequest<GetCustomer360SupplementsResult>, IRequirePermission
{
    public string CustomerId { get; init; } = "";
    public string PermissionKey => PermissionCatalog.CustomerView;
}

public class GetCustomer360SupplementsHandler : IRequestHandler<GetCustomer360SupplementsRequest, GetCustomer360SupplementsResult>
{
    private readonly IMediator _mediator;
    private readonly IQueryContext _query;
    private readonly IUserAuditReadService _auditRead;

    public GetCustomer360SupplementsHandler(IMediator mediator, IQueryContext query, IUserAuditReadService auditRead)
    {
        _mediator = mediator;
        _query = query;
        _auditRead = auditRead;
    }

    public async Task<GetCustomer360SupplementsResult> Handle(
        GetCustomer360SupplementsRequest request,
        CancellationToken cancellationToken)
    {
        var profileIds = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == request.CustomerId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var subscriptionIds = profileIds.Count == 0
            ? new List<string>()
            : await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                .Where(s => profileIds.Contains(s.SubscriberProfileId))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

        var vasServices = subscriptionIds.Count == 0
            ? new List<Customer360VasServiceDto>()
            : await (
                from v in _query.SubscriberActiveService.AsNoTracking().IsDeletedEqualTo()
                join cat in _query.TelecomValueAddedService.AsNoTracking() on v.TelecomValueAddedServiceId equals cat.Id
                where subscriptionIds.Contains(v.TelecomSubscriptionId)
                orderby v.Status descending, v.ActivatedAtUtc descending
                select new Customer360VasServiceDto(
                    v.Id,
                    v.TelecomSubscriptionId,
                    v.Msisdn,
                    cat.NameAr,
                    cat.ServiceCode,
                    v.Status.ToString(),
                    v.ActivatedAtUtc,
                    v.DeactivatedAtUtc))
                .ToListAsync(cancellationToken);

        var billingLogs = await LoadBillingLogsAsync(profileIds, cancellationToken);

        var tickets = await _mediator.Send(
            new GetTechnicalTicketsRequest
            {
                CustomerId = request.CustomerId,
                IncludeResolved = true,
            },
            cancellationToken);

        var audit = await _auditRead.QueryAsync(
            new UserAuditLogQuery
            {
                CustomerId = request.CustomerId,
                Skip = 0,
                Take = 30,
            },
            cancellationToken);

        var operations = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => profileIds.Contains(o.SubscriberProfileId))
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new TelecomOperationListItemDto
            {
                Id = o.Id,
                Number = o.Number,
                Kind = o.Kind.ToString(),
                Status = o.Status.ToString(),
                Msisdn = o.MsisdnAsset != null ? o.MsisdnAsset.Msisdn : "",
                MsisdnAssetId = o.MsisdnAssetId,
                CreatedAtUtc = o.CreatedAtUtc ?? DateTime.MinValue
            })
            .Take(20)
            .ToListAsync(cancellationToken);

        return new GetCustomer360SupplementsResult
        {
            ActiveVasServices = vasServices,
            SupportTickets = tickets.Data ?? new List<TechnicalTicketListItemDto>(),
            BillingLogs = billingLogs,
            ActivityLogs = audit.Items.ToList(),
            TelecomOperations = operations
        };
    }

    internal static async Task<List<GetBillingIntegrationLogListDto>> LoadBillingLogsAsync(
        List<string> profileIds,
        CancellationToken cancellationToken,
        IQueryContext query)
    {
        if (profileIds.Count == 0)
            return new List<GetBillingIntegrationLogListDto>();

        var operationIds = await query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => profileIds.Contains(o.SubscriberProfileId))
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => o.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (operationIds.Count == 0)
            return new List<GetBillingIntegrationLogListDto>();

        var logs = await query.BillingIntegrationLog.AsNoTracking().IsDeletedEqualTo()
            .Where(l => l.TelecomOperationRequestId != null && operationIds.Contains(l.TelecomOperationRequestId))
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        var opNumbers = await query.TelecomOperationRequest.AsNoTracking()
            .Where(o => operationIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Number })
            .ToDictionaryAsync(x => x.Id, x => x.Number ?? "", cancellationToken);

        return logs.Select(x => new GetBillingIntegrationLogListDto
        {
            Id = x.Id,
            TelecomOperationRequestId = x.TelecomOperationRequestId,
            OperationNumber = x.TelecomOperationRequestId != null ? opNumbers.GetValueOrDefault(x.TelecomOperationRequestId) : null,
            AttemptNumber = x.AttemptNumber,
            Success = x.Success,
            Message = x.Message,
            IntegrationTarget = x.IntegrationTarget,
            CreatedAtUtc = x.CreatedAtUtc,
        }).ToList();
    }

    private Task<List<GetBillingIntegrationLogListDto>> LoadBillingLogsAsync(
        List<string> profileIds,
        CancellationToken cancellationToken) =>
        LoadBillingLogsAsync(profileIds, cancellationToken, _query);
}
