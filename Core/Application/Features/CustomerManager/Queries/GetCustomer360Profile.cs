using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Features.TelecomBackOfficeManager.Queries;
using Application.Features.TelecomManager.Queries;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public class GetCustomer360ProfileResult
{
    public GetCustomer360Result Core { get; init; } = null!;
    public List<Customer360VasServiceDto> ActiveVasServices { get; init; } = new();
    public List<TechnicalTicketListItemDto> SupportTickets { get; init; } = new();
    public List<GetBillingIntegrationLogListDto> BillingLogs { get; init; } = new();
    public List<UserAuditLogListItemDto> ActivityLogs { get; init; } = new();
}

public class GetCustomer360ProfileRequest : IRequest<GetCustomer360ProfileResult>
{
    public string CustomerId { get; init; } = "";
}

public class GetCustomer360ProfileHandler : IRequestHandler<GetCustomer360ProfileRequest, GetCustomer360ProfileResult>
{
    private readonly IMediator _mediator;
    private readonly IQueryContext _query;
    private readonly IUserAuditReadService _auditRead;

    public GetCustomer360ProfileHandler(IMediator mediator, IQueryContext query, IUserAuditReadService auditRead)
    {
        _mediator = mediator;
        _query = query;
        _auditRead = auditRead;
    }

    public async Task<GetCustomer360ProfileResult> Handle(
        GetCustomer360ProfileRequest request,
        CancellationToken cancellationToken)
    {
        var core = await _mediator.Send(new GetCustomer360Request { CustomerId = request.CustomerId }, cancellationToken);

        var tickets = await _mediator.Send(
            new GetTechnicalTicketsRequest
            {
                CustomerId = request.CustomerId,
                IncludeResolved = true,
            },
            cancellationToken);

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

        var operationIds = profileIds.Count == 0
            ? new List<string>()
            : await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
                .Where(o => profileIds.Contains(o.SubscriberProfileId))
                .OrderByDescending(o => o.CreatedAtUtc)
                .Select(o => o.Id)
                .Take(50)
                .ToListAsync(cancellationToken);

        var billingLogs = new List<GetBillingIntegrationLogListDto>();
        if (operationIds.Count > 0)
        {
            var logs = await _query.BillingIntegrationLog.AsNoTracking().IsDeletedEqualTo()
                .Where(l => operationIds.Contains(l.TelecomOperationRequestId))
                .OrderByDescending(l => l.CreatedAtUtc)
                .Take(25)
                .ToListAsync(cancellationToken);

            var opNumbers = await _query.TelecomOperationRequest.AsNoTracking()
                .Where(o => operationIds.Contains(o.Id))
                .Select(o => new { o.Id, o.Number })
                .ToDictionaryAsync(x => x.Id, x => x.Number ?? "", cancellationToken);

            billingLogs = logs.Select(x => new GetBillingIntegrationLogListDto
            {
                Id = x.Id,
                TelecomOperationRequestId = x.TelecomOperationRequestId,
                OperationNumber = opNumbers.GetValueOrDefault(x.TelecomOperationRequestId),
                AttemptNumber = x.AttemptNumber,
                Success = x.Success,
                Message = x.Message,
                IntegrationTarget = x.IntegrationTarget,
                CreatedAtUtc = x.CreatedAtUtc,
            }).ToList();
        }

        var audit = await _auditRead.QueryAsync(
            new UserAuditLogQuery
            {
                CustomerId = request.CustomerId,
                Skip = 0,
                Take = 30,
            },
            cancellationToken);

        return new GetCustomer360ProfileResult
        {
            Core = core,
            ActiveVasServices = vasServices,
            SupportTickets = tickets.Data ?? new List<TechnicalTicketListItemDto>(),
            BillingLogs = billingLogs,
            ActivityLogs = audit.Items.ToList(),
        };
    }
}
