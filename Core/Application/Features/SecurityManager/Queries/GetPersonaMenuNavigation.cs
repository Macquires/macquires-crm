using Application.Common.Services.SecurityManager;
using Application.Common.Telecom;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetPersonaMenuNavigationResult
{
    public List<MenuNavigationTreeNodeDto> Data { get; init; } = new();
    public string? PrimaryMenuPersona { get; init; }
}

public class GetPersonaMenuNavigationRequest : IRequest<GetPersonaMenuNavigationResult>
{
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public string? PreviewPersona { get; init; }
}

public class GetPersonaMenuNavigationValidator : AbstractValidator<GetPersonaMenuNavigationRequest>
{
    public GetPersonaMenuNavigationValidator()
    {
        RuleFor(x => x.Roles).NotEmpty();
    }
}

public class GetPersonaMenuNavigationHandler : IRequestHandler<GetPersonaMenuNavigationRequest, GetPersonaMenuNavigationResult>
{
    private readonly INavigationMenuService _navigationMenu;

    public GetPersonaMenuNavigationHandler(INavigationMenuService navigationMenu) =>
        _navigationMenu = navigationMenu;

    public Task<GetPersonaMenuNavigationResult> Handle(
        GetPersonaMenuNavigationRequest request,
        CancellationToken cancellationToken)
    {
        var canPreview = request.Roles.Any(r =>
            string.Equals(r, "TelecomAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "TelecomManagement", StringComparison.OrdinalIgnoreCase));

        TelecomMenuPersona? preview = null;
        if (canPreview
            && !string.IsNullOrWhiteSpace(request.PreviewPersona)
            && Enum.TryParse<TelecomMenuPersona>(request.PreviewPersona, true, out var parsed))
        {
            preview = parsed;
        }

        var nodes = _navigationMenu.GetMenuForRoles(request.Roles, preview);
        var primary = preview ?? TelecomPersonaResolver.ResolvePrimary(request.Roles);

        return Task.FromResult(new GetPersonaMenuNavigationResult
        {
            Data = nodes,
            PrimaryMenuPersona = primary?.ToString(),
        });
    }
}
