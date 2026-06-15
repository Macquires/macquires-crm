using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Application.Features.TelecomManager.Queries;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetMenuBadgesResult
{
    public MenuBadgesDto Data { get; init; } = new();
}

public class GetMenuBadgesRequest : IRequest<GetMenuBadgesResult>, IRequireAuthenticatedOperator;

public class GetMenuBadgesHandler : IRequestHandler<GetMenuBadgesRequest, GetMenuBadgesResult>
{
    private readonly INavigationMenuService _navigationMenu;
    private readonly IMediator _mediator;
    private readonly IPermissionEvaluator _permissions;
    private readonly IOperatorContext _operator;

    public GetMenuBadgesHandler(
        INavigationMenuService navigationMenu,
        IMediator mediator,
        IPermissionEvaluator permissions,
        IOperatorContext operatorContext)
    {
        _navigationMenu = navigationMenu;
        _mediator = mediator;
        _permissions = permissions;
        _operator = operatorContext;
    }

    public async Task<GetMenuBadgesResult> Handle(GetMenuBadgesRequest request, CancellationToken cancellationToken)
    {
        var data = await _navigationMenu.GetMenuBadgesAsync(cancellationToken);
        var criticalExceptions = 0;

        var userId = _operator.UserId;
        if (!string.IsNullOrEmpty(userId)
            && await _permissions.HasPermissionAsync(userId, PermissionCatalog.TelecomReportsMis, cancellationToken))
        {
            try
            {
                var exceptions = await _mediator.Send(new GetExecutiveExceptionsRequest(null, null), cancellationToken);
                criticalExceptions = exceptions.CriticalCount;
            }
            catch
            {
                criticalExceptions = 0;
            }
        }

        return new GetMenuBadgesResult
        {
            Data = new MenuBadgesDto
            {
                PendingOperations = data.PendingOperations,
                OverdueTickets = data.OverdueTickets,
                OpenTechnicalTickets = data.OpenTechnicalTickets,
                BulkImportActive = data.BulkImportActive,
                CriticalExecutiveExceptions = criticalExceptions,
            },
        };
    }
}
