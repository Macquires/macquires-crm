using Application.Common.Dashboard;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using MediatR;

namespace Application.Features.DashboardManager.Queries;

public class GetMyDashboardWidgetsResult
{
    public IReadOnlyList<DashboardWidgetDefinitionDto> Data { get; init; } = Array.Empty<DashboardWidgetDefinitionDto>();
    public string? PrimaryMenuPersona { get; init; }
}

public class GetMyDashboardWidgetsRequest : IRequest<GetMyDashboardWidgetsResult>
{
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public string? PreviewPersona { get; init; }
}

public class GetMyDashboardWidgetsHandler : IRequestHandler<GetMyDashboardWidgetsRequest, GetMyDashboardWidgetsResult>
{
    private readonly IDashboardWidgetCatalogReader _catalog;
    private readonly IDashboardWidgetRegistry _registry;
    private readonly IOperatorContext _operator;

    public GetMyDashboardWidgetsHandler(
        IDashboardWidgetCatalogReader catalog,
        IDashboardWidgetRegistry registry,
        IOperatorContext operatorContext)
    {
        _catalog = catalog;
        _registry = registry;
        _operator = operatorContext;
    }

    public async Task<GetMyDashboardWidgetsResult> Handle(
        GetMyDashboardWidgetsRequest request,
        CancellationToken cancellationToken)
    {
        var preview = CanPreviewPersona(request.Roles)
            ? request.PreviewPersona ?? _operator.EffectivePersona?.ToString()
            : null;
        var persona = DashboardPersonaFilter.ResolveEffectivePersona(request.Roles, preview);
        if (persona == null)
        {
            return new GetMyDashboardWidgetsResult();
        }

        var personaName = persona.Value.ToString();
        var rows = await _catalog.GetActiveWidgetsAsync(CancellationToken.None);

        var filtered = rows
            .Where(w => DashboardPersonaFilter.WidgetVisibleForPersona(w.PersonasAllowed, personaName))
            .Where(w => string.IsNullOrWhiteSpace(w.ProviderKey) || _registry.IsRegistered(w.ProviderKey))
            .Select(Map)
            .ToList();

        return new GetMyDashboardWidgetsResult
        {
            Data = filtered,
            PrimaryMenuPersona = personaName,
        };
    }

    private static bool CanPreviewPersona(IReadOnlyList<string> roles) =>
        roles.Any(r =>
            string.Equals(r, "TelecomAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "TelecomManagement", StringComparison.OrdinalIgnoreCase));

    private static DashboardWidgetDefinitionDto Map(DashboardWidget w) => new()
    {
        Id = w.Id,
        WidgetKey = w.WidgetKey,
        TitleAr = w.TitleAr,
        TitleEn = w.TitleEn,
        Icon = w.Icon,
        ProviderKey = w.ProviderKey,
        GridSize = w.GridSize,
        SortOrder = w.SortOrder,
        WidgetKind = w.WidgetKind,
        RefreshIntervalSeconds = w.RefreshIntervalSeconds,
        CtaUrl = w.CtaUrl,
        CtaLabelAr = w.CtaLabelAr,
        CtaLabelEn = w.CtaLabelEn,
    };
}
