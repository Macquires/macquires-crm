using Application.Common.Dashboard;
using Application.Common.Security;
using MediatR;

namespace Application.Features.DashboardManager.Queries;

public class GetDashboardWidgetDataResult
{
    public DashboardWidgetDataDto Data { get; init; } = null!;
}

public class GetDashboardWidgetDataRequest : IRequest<GetDashboardWidgetDataResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public string ProviderKey { get; init; } = null!;
    public string? PreviewPersona { get; init; }
    public IReadOnlyList<string> PermissionKeys => DashboardPermissionSets.ReadAny;
}

public class GetDashboardWidgetDataHandler : IRequestHandler<GetDashboardWidgetDataRequest, GetDashboardWidgetDataResult>
{
    private readonly IDashboardWidgetCatalogReader _catalog;
    private readonly IDashboardWidgetRegistry _registry;
    private readonly IOperatorContext _operator;

    public GetDashboardWidgetDataHandler(
        IDashboardWidgetCatalogReader catalog,
        IDashboardWidgetRegistry registry,
        IOperatorContext operatorContext)
    {
        _catalog = catalog;
        _registry = registry;
        _operator = operatorContext;
    }

    public async Task<GetDashboardWidgetDataResult> Handle(
        GetDashboardWidgetDataRequest request,
        CancellationToken cancellationToken)
    {
        var preview = CanPreviewPersona(request.Roles)
            ? request.PreviewPersona ?? _operator.EffectivePersona?.ToString()
            : null;
        await EnsureProviderAllowedForUser(request.Roles, preview, request.ProviderKey, cancellationToken);

        var data = await _registry.GetDataAsync(request.ProviderKey, CancellationToken.None);
        return new GetDashboardWidgetDataResult { Data = data };
    }

    private async Task EnsureProviderAllowedForUser(
        IReadOnlyList<string> roles,
        string? previewPersona,
        string providerKey,
        CancellationToken cancellationToken)
    {
        var persona = DashboardPersonaFilter.ResolveEffectivePersona(roles, previewPersona)
            ?? throw new UnauthorizedAccessException("No telecom persona for dashboard data.");

        var personaName = persona.ToString()!;
        var widgets = await _catalog.GetActiveWidgetsAsync(CancellationToken.None);
        var allowedForPersona = widgets
            .Where(w => string.Equals(w.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase))
            .Any(w => DashboardPersonaFilter.WidgetVisibleForPersona(w.PersonasAllowed, personaName));

        if (!allowedForPersona)
        {
            throw new UnauthorizedAccessException($"Provider '{providerKey}' is not visible for this persona.");
        }

        if (!_registry.IsRegistered(providerKey))
        {
            throw new InvalidOperationException($"Provider '{providerKey}' is not registered.");
        }
    }

    private static bool CanPreviewPersona(IReadOnlyList<string> roles) =>
        roles.Any(r =>
            string.Equals(r, "TelecomAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "TelecomManagement", StringComparison.OrdinalIgnoreCase));
}
