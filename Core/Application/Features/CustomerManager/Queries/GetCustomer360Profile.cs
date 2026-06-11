using Application.Common.Audit;
using Application.Features.TelecomBackOfficeManager.Queries;
using Application.Features.TelecomManager.Queries;
using MediatR;

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

    public GetCustomer360ProfileHandler(IMediator mediator) => _mediator = mediator;

    public async Task<GetCustomer360ProfileResult> Handle(
        GetCustomer360ProfileRequest request,
        CancellationToken cancellationToken)
    {
        var core = await _mediator.Send(new GetCustomer360Request { CustomerId = request.CustomerId }, cancellationToken);
        var supplements = await _mediator.Send(
            new GetCustomer360SupplementsRequest { CustomerId = request.CustomerId },
            cancellationToken);

        return new GetCustomer360ProfileResult
        {
            Core = core,
            ActiveVasServices = supplements.ActiveVasServices,
            SupportTickets = supplements.SupportTickets,
            BillingLogs = supplements.BillingLogs,
            ActivityLogs = supplements.ActivityLogs,
        };
    }
}
