using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Settings;
using Application.Common.Telecom;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;

public class UpdateGlobalSettingsResult
{
    public GlobalSettingsSnapshotDto? Data { get; init; }
    public PendingExternalSyncSummaryDto? PendingSync { get; init; }
}

public sealed class PendingExternalSyncSummaryDto
{
    public int Processed { get; init; }
    public int Succeeded { get; init; }
    public int StillPending { get; init; }
    public int Failed { get; init; }
}

public class UpdateGlobalSettingsRequest : IRequest<UpdateGlobalSettingsResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.AdminSettingsManage;
    public GlobalSettingsSnapshotDto? Settings { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateGlobalSettingsValidator : AbstractValidator<UpdateGlobalSettingsRequest>
{
    public UpdateGlobalSettingsValidator() => RuleFor(x => x.Settings).NotNull();
}

public class UpdateGlobalSettingsHandler : IRequestHandler<UpdateGlobalSettingsRequest, UpdateGlobalSettingsResult>
{
    private readonly IGlobalSettingsAdminService _admin;
    private readonly IUserAuditService _audit;
    private readonly IPendingExternalSyncService _pendingSync;

    public UpdateGlobalSettingsHandler(
        IGlobalSettingsAdminService admin,
        IUserAuditService audit,
        IPendingExternalSyncService pendingSync)
    {
        _admin = admin;
        _audit = audit;
        _pendingSync = pendingSync;
    }

    public async Task<UpdateGlobalSettingsResult> Handle(UpdateGlobalSettingsRequest request, CancellationToken cancellationToken)
    {
        var before = await _admin.GetSnapshotAsync(cancellationToken);
        var incoming = request.Settings!;

        await _admin.SaveSnapshotAsync(incoming, request.UpdatedById, cancellationToken);
        var data = await _admin.GetSnapshotAsync(cancellationToken);

        await LogIntegrationToggleChangesAsync(before, data, request.UpdatedById, cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.UpdatedById ?? "system",
                ActionType = UserAuditActionTypes.GlobalSettingsUpdated,
                EntityType = "GlobalSetting",
                SummaryAr = "تحديث الإعدادات العامة",
                Payload = data,
            },
            cancellationToken);

        PendingExternalSyncSummaryDto? syncSummary = null;
        if (IntegrationTurnedOn(before.IntegrationHuaweiEnabled, data.IntegrationHuaweiEnabled)
            || IntegrationTurnedOn(before.IntegrationHlrEnabled, data.IntegrationHlrEnabled)
            || IntegrationTurnedOn(before.IntegrationSmsEnabled, data.IntegrationSmsEnabled))
        {
            var flush = await _pendingSync.FlushAsync(request.UpdatedById, cancellationToken);
            syncSummary = new PendingExternalSyncSummaryDto
            {
                Processed = flush.Processed,
                Succeeded = flush.Succeeded,
                StillPending = flush.StillPending,
                Failed = flush.Failed,
            };
        }

        return new UpdateGlobalSettingsResult { Data = data, PendingSync = syncSummary };
    }

    private async Task LogIntegrationToggleChangesAsync(
        GlobalSettingsSnapshotDto before,
        GlobalSettingsSnapshotDto after,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        await LogToggleIfChangedAsync("Huawei CBS", before.IntegrationHuaweiEnabled, after.IntegrationHuaweiEnabled, actorUserId, cancellationToken);
        await LogToggleIfChangedAsync("HLR", before.IntegrationHlrEnabled, after.IntegrationHlrEnabled, actorUserId, cancellationToken);
        await LogToggleIfChangedAsync("SMS Gateway", before.IntegrationSmsEnabled, after.IntegrationSmsEnabled, actorUserId, cancellationToken);
    }

    private async Task LogToggleIfChangedAsync(
        string gateway,
        bool wasEnabled,
        bool isEnabled,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (wasEnabled == isEnabled) return;

        var state = isEnabled ? "ON" : "OFF";
        var modeAr = isEnabled ? "تشغيل حي" : "وضع الطوارئ (محاكاة)";

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId ?? "system",
                ActionType = UserAuditActionTypes.IntegrationCircuitBreakerChanged,
                EntityType = "GlobalSetting",
                SummaryAr = $"تغيير تكامل {gateway} إلى {state} — {modeAr}",
                Payload = new { gateway, state, isEnabled, modeAr },
            },
            cancellationToken);
    }

    private static bool IntegrationTurnedOn(bool wasEnabled, bool isEnabled) => !wasEnabled && isEnabled;
}
