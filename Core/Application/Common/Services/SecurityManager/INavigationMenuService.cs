namespace Application.Common.Services.SecurityManager;

public interface INavigationMenuService
{
    List<MenuNavigationTreeNodeDto> GetMenuForRoles(
        IReadOnlyList<string> roleNames,
        TelecomMenuPersona? previewPersona = null,
        IReadOnlySet<string>? permissionKeys = null);

    Task<MenuBadgesDto> GetMenuBadgesAsync(CancellationToken cancellationToken = default);
}
