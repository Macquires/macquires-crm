using Application.Common.Dashboard;
using MediatR;

namespace Application.Features.DashboardManager.Queries;

public class GetRegisteredDashboardProvidersResult
{
    public IReadOnlyList<string> Data { get; init; } = Array.Empty<string>();
}

public class GetRegisteredDashboardProvidersRequest : IRequest<GetRegisteredDashboardProvidersResult>;

public class GetRegisteredDashboardProvidersHandler
    : IRequestHandler<GetRegisteredDashboardProvidersRequest, GetRegisteredDashboardProvidersResult>
{
    private readonly IDashboardWidgetRegistry _registry;

    public GetRegisteredDashboardProvidersHandler(IDashboardWidgetRegistry registry) => _registry = registry;

    public Task<GetRegisteredDashboardProvidersResult> Handle(
        GetRegisteredDashboardProvidersRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new GetRegisteredDashboardProvidersResult
        {
            Data = _registry.GetRegisteredProviderKeys(),
        });
}
