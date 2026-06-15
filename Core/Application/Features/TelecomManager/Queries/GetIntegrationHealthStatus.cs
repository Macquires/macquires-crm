using Application.Common.Security;
using Application.Common.Settings;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public sealed class IntegrationHealthItemDto
{
    public string System { get; init; } = null!;
    public string Mode { get; init; } = null!;
    public string LabelAr { get; init; } = null!;
    public string LabelEn { get; init; } = null!;
}

public class GetIntegrationHealthStatusResult
{
    public IReadOnlyList<IntegrationHealthItemDto> Items { get; init; } = Array.Empty<IntegrationHealthItemDto>();
}

public class GetIntegrationHealthStatusRequest : IRequest<GetIntegrationHealthStatusResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => IntegrationMonitorPermissionSets.MonitorAny;
}

public class GetIntegrationHealthStatusHandler : IRequestHandler<GetIntegrationHealthStatusRequest, GetIntegrationHealthStatusResult>
{
    private readonly IGlobalSettingsProvider _settings;

    public GetIntegrationHealthStatusHandler(IGlobalSettingsProvider settings) => _settings = settings;

    public async Task<GetIntegrationHealthStatusResult> Handle(
        GetIntegrationHealthStatusRequest request,
        CancellationToken cancellationToken)
    {
        var huawei = await _settings.GetBoolAsync(GlobalSettingKeys.IntegrationHuaweiEnabled, true, cancellationToken);
        var intelligentNetwork = await _settings.GetBoolAsync(GlobalSettingKeys.IntegrationInEnabled, true, cancellationToken);
        var hlr = await _settings.GetBoolAsync(GlobalSettingKeys.IntegrationHlrEnabled, true, cancellationToken);
        var sms = await _settings.GetBoolAsync(GlobalSettingKeys.IntegrationSmsEnabled, true, cancellationToken);

        return new GetIntegrationHealthStatusResult
        {
            Items =
            [
                Map("Huawei_CBS", "Huawei CBS", huawei),
                Map("Huawei_IN", "Huawei IN", intelligentNetwork),
                Map("Huawei_HLR", "HLR", hlr),
                Map("SmsGateway", "SMS Gateway", sms),
            ],
        };
    }

    private static IntegrationHealthItemDto Map(string system, string displayName, bool enabled) =>
        enabled
            ? new IntegrationHealthItemDto
            {
                System = system,
                Mode = "live",
                LabelAr = $"{displayName}: متصل (تشغيل حي)",
                LabelEn = $"{displayName}: System Live",
            }
            : new IntegrationHealthItemDto
            {
                System = system,
                Mode = "fallback",
                LabelAr = $"{displayName}: محاكاة طوارئ (Fallback)",
                LabelEn = $"{displayName}: Fallback Simulation Active (Emergency Mode)",
            };
}
