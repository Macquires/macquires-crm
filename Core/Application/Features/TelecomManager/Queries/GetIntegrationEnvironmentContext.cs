using Application.Common.Security;
using Application.Common.Settings;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public sealed class IntegrationEnvironmentContextDto
{
    public bool IsDemoVersion { get; init; }
}

public class GetIntegrationEnvironmentContextResult
{
    public IntegrationEnvironmentContextDto? Data { get; init; }
}

public class GetIntegrationEnvironmentContextRequest : IRequest<GetIntegrationEnvironmentContextResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => DashboardPermissionSets.ReadAny;
}

public class GetIntegrationEnvironmentContextHandler
    : IRequestHandler<GetIntegrationEnvironmentContextRequest, GetIntegrationEnvironmentContextResult>
{
    private readonly IGlobalSettingsProvider _settings;

    public GetIntegrationEnvironmentContextHandler(IGlobalSettingsProvider settings) => _settings = settings;

    public async Task<GetIntegrationEnvironmentContextResult> Handle(
        GetIntegrationEnvironmentContextRequest request,
        CancellationToken cancellationToken)
    {
        var isDemo = await _settings.GetBoolAsync(GlobalSettingKeys.IsDemoVersion, true, cancellationToken);
        return new GetIntegrationEnvironmentContextResult
        {
            Data = new IntegrationEnvironmentContextDto { IsDemoVersion = isDemo },
        };
    }
}
