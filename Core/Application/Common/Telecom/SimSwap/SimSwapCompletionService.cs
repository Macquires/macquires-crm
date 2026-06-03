using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

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
    private readonly ISmsGatewayIntegration _sms;
    private readonly IUnitOfWork _unitOfWork;

    public SimSwapCompletionService(
        IQueryContext query,
        ICommandRepository<TelecomOperationAuditLog> auditRepository,
        ISmsGatewayIntegration sms,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _auditRepository = auditRepository;
        _sms = sms;
        _unitOfWork = unitOfWork;
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

        if (!string.IsNullOrEmpty(msisdn))
        {
            var reason = operation.ReplacementReason ?? "طلب مشترك";
            var body =
                $"سيريتل: تم تفعيل شريحتك الجديدة على الخط {msisdn}. " +
                $"السبب: {reason}. مرجع العملية {operation.Number}.";
            await _sms.SendAsync(msisdn, body, cancellationToken);
        }
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
