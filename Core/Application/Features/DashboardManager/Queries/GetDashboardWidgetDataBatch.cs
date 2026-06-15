using Application.Common.Dashboard;
using Application.Common.Security;
using MediatR;

namespace Application.Features.DashboardManager.Queries;

public class GetDashboardWidgetDataBatchResult
{
    public IReadOnlyDictionary<string, DashboardWidgetDataDto> Data { get; init; }
        = new Dictionary<string, DashboardWidgetDataDto>();
}

public class GetDashboardWidgetDataBatchRequest : IRequest<GetDashboardWidgetDataBatchResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ProviderKeys { get; init; } = Array.Empty<string>();
    public string? PreviewPersona { get; init; }
    public IReadOnlyList<string> PermissionKeys => DashboardPermissionSets.ReadAny;
}

public class GetDashboardWidgetDataBatchHandler : IRequestHandler<GetDashboardWidgetDataBatchRequest, GetDashboardWidgetDataBatchResult>
{
    private readonly IDashboardWidgetCatalogReader _catalog;
    private readonly IDashboardWidgetRegistry _registry;
    private readonly IOperatorContext _operator;

    public GetDashboardWidgetDataBatchHandler(
        IDashboardWidgetCatalogReader catalog,
        IDashboardWidgetRegistry registry,
        IOperatorContext operatorContext)
    {
        _catalog = catalog;
        _registry = registry;
        _operator = operatorContext;
    }

    public async Task<GetDashboardWidgetDataBatchResult> Handle(
        GetDashboardWidgetDataBatchRequest request,
        CancellationToken cancellationToken)
    {
        var preview = CanPreviewPersona(request.Roles)
            ? request.PreviewPersona ?? _operator.EffectivePersona?.ToString()
            : null;
        var persona = DashboardPersonaFilter.ResolveEffectivePersona(request.Roles, preview);
        if (persona == null || request.ProviderKeys.Count == 0)
        {
            return new GetDashboardWidgetDataBatchResult();
        }

        var personaName = persona.Value.ToString();
        var keySet = request.ProviderKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var widgets = (await _catalog.GetActiveWidgetsAsync(CancellationToken.None))
            .Where(w => w.ProviderKey != null && keySet.Contains(w.ProviderKey))
            .ToList();

        var visible = widgets
            .Where(w => DashboardPersonaFilter.WidgetVisibleForPersona(w.PersonasAllowed, personaName))
            .Select(w => w.ProviderKey!)
            .Where(k => _registry.IsRegistered(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dict = new Dictionary<string, DashboardWidgetDataDto>(StringComparer.OrdinalIgnoreCase);
        // EF DbContext is not safe for parallel use — load providers one at a time per request scope.
        foreach (var key in visible)
        {
            dict[key] = await _registry.GetDataAsync(key, CancellationToken.None);
        }

        return new GetDashboardWidgetDataBatchResult { Data = dict };
    }

    private static bool CanPreviewPersona(IReadOnlyList<string> roles) =>
        roles.Any(r =>
            string.Equals(r, "TelecomAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "TelecomManagement", StringComparison.OrdinalIgnoreCase));
}
