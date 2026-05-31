using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Application.Common.Telecom;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetOperatorSessionResult
{
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public string LandingPath { get; init; } = PermissionLandingResolver.MyProfile;
    public List<MenuNavigationTreeNodeDto> MenuNavigation { get; init; } = [];
    public List<string> Roles { get; init; } = [];
    public string? PrimaryMenuPersona { get; init; }
}

public class GetOperatorSessionRequest : IRequest<GetOperatorSessionResult>
{
    public string UserId { get; init; } = null!;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public string? PrimaryMenuPersona { get; init; }
}

public class GetOperatorSessionHandler : IRequestHandler<GetOperatorSessionRequest, GetOperatorSessionResult>
{
    private readonly IPermissionEvaluator _permissions;
    private readonly INavigationMenuService _navigationMenu;

    public GetOperatorSessionHandler(IPermissionEvaluator permissions, INavigationMenuService navigationMenu)
    {
        _permissions = permissions;
        _navigationMenu = navigationMenu;
    }

    public async Task<GetOperatorSessionResult> Handle(
        GetOperatorSessionRequest request,
        CancellationToken cancellationToken)
    {
        var rolesList = (await _permissions.GetUserRoleNamesAsync(request.UserId, cancellationToken)).ToList();
        if (rolesList.Count == 0 && request.Roles.Count > 0)
        {
            rolesList = request.Roles.ToList();
        }
        var permissionList = await _permissions.GetUserPermissionKeysAsync(request.UserId, cancellationToken);
        var permissionSet = permissionList.ToHashSet(StringComparer.OrdinalIgnoreCase);

        TelecomMenuPersona? explicitPersona = null;
        if (!string.IsNullOrWhiteSpace(request.PrimaryMenuPersona)
            && Enum.TryParse<TelecomMenuPersona>(request.PrimaryMenuPersona, true, out var parsed))
        {
            explicitPersona = parsed;
        }

        var primaryPersona = TelecomPersonaResolver.ResolvePrimary(rolesList, explicitPersona);
        var menu = _navigationMenu.GetMenuForRoles(rolesList, permissionKeys: permissionSet);

        return new GetOperatorSessionResult
        {
            Permissions = permissionList,
            LandingPath = TelecomWorkspaceRules.ResolveSessionLandingPath(
                rolesList,
                primaryPersona,
                permissionSet),
            MenuNavigation = menu,
            Roles = rolesList,
            PrimaryMenuPersona = primaryPersona?.ToString(),
        };
    }
}
