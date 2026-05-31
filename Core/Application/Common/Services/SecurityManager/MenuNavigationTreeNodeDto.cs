namespace Application.Common.Services.SecurityManager;

public class MenuNavigationTreeNodeDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    /// <summary>English label for sidebar (telecom-style copy). When null, clients may fall back to <see cref="Name"/>.</summary>
    public string? NameEn { get; set; }
    public string? Pid { get; set; }
    public string? NavURL { get; set; }
    public bool HasChild { get; set; }
    public bool Expanded { get; set; }
    public bool IsSelected { get; set; }
    /// <summary>Personas allowed to see this node (<see cref="TelecomMenuPersona"/> names). Null/empty = not persona-gated.</summary>
    public IReadOnlyList<string>? Personas { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public string? BadgeKey { get; set; }
    public bool IsQuickAction { get; set; }

    public MenuNavigationTreeNodeDto(
        string param_id,
        string param_name,
        string? param_pid = null,
        string? param_navURL = null,
        bool param_hasChild = false,
        bool param_expanded = false,
        bool param_selected = false,
        string? param_nameEn = null,
        IReadOnlyList<string>? param_personas = null,
        string? param_icon = null,
        int param_sortOrder = 0,
        string? param_badgeKey = null,
        bool param_isQuickAction = false)
    {
        Id = param_id;
        Name = param_name;
        NameEn = param_nameEn;
        Pid = param_pid;
        NavURL = param_navURL;
        HasChild = param_hasChild;
        Expanded = param_expanded;
        IsSelected = param_selected;
        Personas = param_personas;
        Icon = param_icon;
        SortOrder = param_sortOrder;
        BadgeKey = param_badgeKey;
        IsQuickAction = param_isQuickAction;
    }
}
