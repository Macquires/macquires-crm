using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Common.Telecom.SimSwap;

public interface ISimSwapCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class SimSwapCompletionService : ISimSwapCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IHLRLiveStatusService _hlr;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SimSwapCompletionService> _logger;

    public SimSwapCompletionService(
        IQueryContext query,
        ICommandRepository<TelecomOperationAuditLog> auditRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ISmsGatewayIntegration sms,
        IHLRLiveStatusService hlr,
        IUnitOfWork unitOfWork,
        ILogger<SimSwapCompletionService> logger)
    {
        _query = query;
        _auditRepository = auditRepository;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _sms = sms;
        _hlr = hlr;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.SimSwap)
        {
            return;
        }

        var priorIccid = await ResolveIccidAsync(operation.PriorSimInventoryId, cancellationToken);
        var newIccid = await ResolveIccidAsync(operation.SimInventoryId, cancellationToken);

        var snapshot = new
        {
            operation.Number,
            operation.CorrelationId,
            operation.ReplacementReason,
            operation.IsLostOrStolenReport,
            operation.PriorSimInventoryId,
            operation.SimInventoryId,
            priorIccid,
            newIccid,
            msisdn,
            completedAtUtc = DateTime.UtcNow,
        };

        await _auditRepository.CreateAsync(new TelecomOperationAuditLog
        {
            TelecomOperationRequestId = operation.Id,
            FromStatus = TelecomOperationStatus.Provisioning,
            ToStatus = TelecomOperationStatus.Completed,
            ActorUserId = actorUserId,
            Note = "SIM snapshot — الصندوق الأسود لتبديل الشريحة",
            OccurredAtUtc = DateTime.UtcNow,
            CorrelationId = operation.CorrelationId,
            FieldChangesJson = JsonSerializer.Serialize(snapshot),
            CreatedById = actorUserId,
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (operation.IsLostOrStolenReport)
        {
            await TryAutoResumeAfterLostStolenSwapAsync(operation, msisdn, actorUserId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(msisdn))
        {
            var reason = operation.ReplacementReason ?? "طلب مشترك";
            var body =
                $"سيريتل: تم تفعيل شريحتك الجديدة على الخط {msisdn}. " +
                $"السبب: {reason}. مرجع العملية {operation.Number}.";
            await _sms.SendAsync(msisdn, body, cancellationToken);
        }
    }

    private async Task TryAutoResumeAfterLostStolenSwapAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetAsync(operation.SubscriberProfileId, cancellationToken);
        if (profile == null)
        {
            return;
        }

        var wasSuspended = profile.OperationalStatus is SubscriberOperationalStatus.Suspended
            or SubscriberOperationalStatus.SuspendedInbound
            or SubscriberOperationalStatus.SuspendedOutbound;

        MsisdnAsset? asset = null;
        if (!string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            asset = await _msisdnRepository.GetAsync(operation.MsisdnAssetId, cancellationToken);
            wasSuspended = wasSuspended || asset?.PoolStatus == MsisdnPoolStatus.Suspended;
        }

        if (!wasSuspended)
        {
            return;
        }

        profile.Activate();
        profile.UpdatedById = actorUserId;
        _profileRepository.Update(profile);

        if (asset != null && asset.PoolStatus == MsisdnPoolStatus.Suspended)
        {
            asset.TransitionTo(MsisdnPoolStatus.Active);
            asset.UpdatedById = actorUserId;
            _msisdnRepository.Update(asset);
        }

        operation.ProvisioningResult = "CompletedWithAutoResume";
        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(msisdn))
        {
            await _hlr.MarkMockSubscriberActiveAsync(msisdn, cancellationToken);
        }

        _logger.LogInformation(
            "SIM swap lost/stolen auto-resume applied for operation {OperationId} MSISDN {Msisdn}",
            operation.Id,
            msisdn ?? "—");
    }

    private async Task<string?> ResolveIccidAsync(string? simInventoryId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(simInventoryId))
        {
            return null;
        }

        return await _query.SimInventory.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Id == simInventoryId)
            .Select(s => s.Iccid)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
