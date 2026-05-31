using Application.Common.Services.SecurityManager;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetMenuBadgesResult
{
    public MenuBadgesDto Data { get; init; } = new();
}

public class GetMenuBadgesRequest : IRequest<GetMenuBadgesResult>;

public class GetMenuBadgesHandler : IRequestHandler<GetMenuBadgesRequest, GetMenuBadgesResult>
{
    private readonly INavigationMenuService _navigationMenu;

    public GetMenuBadgesHandler(INavigationMenuService navigationMenu) => _navigationMenu = navigationMenu;

    public async Task<GetMenuBadgesResult> Handle(GetMenuBadgesRequest request, CancellationToken cancellationToken)
    {
        var data = await _navigationMenu.GetMenuBadgesAsync(cancellationToken);
        return new GetMenuBadgesResult { Data = data };
    }
}
