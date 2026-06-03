using Application.Common.CQS.Queries;
using Application.Common.Events;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Common.Telecom.ChangeGsm;
using Application.Common.Telecom.ChangeNumber;
using Application.Common.Telecom.SimSwap;
using Application.Common.Telecom.TakeOver;
using Application.Common.Telecom.Termination;
using Application.Common.Telecom.Suspension;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.OfferSubscription;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.TelecomManager.Events;

/// <summary>HLR provisioning after local confirm + CBS for all lifecycle kinds.</summary>
public sealed class TelecomOperationHlrHandler : INotificationHandler<TelecomOperationProvisionedNotification>
{
    private readonly INetworkProvisioningService _network;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ITelecomOperationOrchestrator _orchestrator;
    private readonly ITelecomHlrFailureCompensator _hlrFailureCompensator;
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TelecomOperationHlrHandler> _logger;
    private readonly ITechnicalTicketQueueIngestionService _ticketQueue;
    private readonly IChangeGsmCompletionService _changeGsmCompletion;
    private readonly ITakeOverCompletionService _takeOverCompletion;
    private readonly ISimSwapCompletionService _simSwapCompletion;
    private readonly IChangeNumberCompletionService _changeNumberCompletion;
    private readonly ITerminationCompletionService _terminationCompletion;
    private readonly ISuspensionCompletionService _suspensionCompletion;
    private readonly IReconnectCompletionService _reconnectCompletion;
    private readonly IMigrationCompletionService _migrationCompletion;

    public TelecomOperationHlrHandler(
        INetworkProvisioningService network,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ITelecomOperationOrchestrator orchestrator,
        ITelecomHlrFailureCompensator hlrFailureCompensator,
        IQueryContext query,
        IUnitOfWork unitOfWork,
        ILogger<TelecomOperationHlrHandler> logger,
        ITechnicalTicketQueueIngestionService ticketQueue,
        IChangeGsmCompletionService changeGsmCompletion,
        ITakeOverCompletionService takeOverCompletion,
        ISimSwapCompletionService simSwapCompletion,
        IChangeNumberCompletionService changeNumberCompletion,
        ITerminationCompletionService terminationCompletion,
        ISuspensionCompletionService suspensionCompletion,
        IReconnectCompletionService reconnectCompletion,
        IMigrationCompletionService migrationCompletion)
    {
        _network = network;
        _operationRepository = operationRepository;
        _orchestrator = orchestrator;
        _hlrFailureCompensator = hlrFailureCompensator;
        _query = query;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _ticketQueue = ticketQueue;
        _changeGsmCompletion = changeGsmCompletion;
        _takeOverCompletion = takeOverCompletion;
        _simSwapCompletion = simSwapCompletion;
        _changeNumberCompletion = changeNumberCompletion;
        _terminationCompletion = terminationCompletion;
        _suspensionCompletion = suspensionCompletion;
        _reconnectCompletion = reconnectCompletion;
        _migrationCompletion = migrationCompletion;
    }

    public async Task Handle(TelecomOperationProvisionedNotification notification, CancellationToken cancellationToken)
    {
        var op = await _operationRepository.GetAsync(notification.TelecomOperationRequestId, cancellationToken);
        if (op == null) return;

        var lineContext = await TelecomProvisionRequestBuilder.ResolveLineContextAsync(_query, op, cancellationToken);
        var request = TelecomProvisionRequestBuilder.ToNetworkRequest(op, lineContext with
        {
            Msisdn = notification.Msisdn ?? lineContext.Msisdn,
            Iccid = notification.Iccid ?? lineContext.Iccid,
        });

        var result = await _network.ProvisionAsync(request, cancellationToken);

        if (result.DeferRetry || result.QueuedForSync)
        {
            await _orchestrator.TransitionAsync(
                op,
                TelecomOperationStatus.PendingExternal,
                notification.ActorUserId,
                result.Message ?? "مزامنة HLR معلّقة",
                cancellationToken);
        }
        else if (result.Success)
        {
            await _orchestrator.TransitionAsync(
                op,
                TelecomOperationStatus.Completed,
                notification.ActorUserId,
                "اكتمل تزويد HLR",
                cancellationToken);

            if (op.Kind == TelecomOperationKind.ChangeGsmType)
            {
                await _changeGsmCompletion.NotifyAndAuditAsync(
                    op,
                    notification.Msisdn ?? lineContext.Msisdn,
                    notification.ActorUserId,
                    cancellationToken);
            }
            else if (op.Kind == TelecomOperationKind.TakeOver)
            {
                await _takeOverCompletion.NotifyAndAuditAsync(
                    op,
                    notification.Msisdn ?? lineContext.Msisdn,
                    notification.ActorUserId,
                    cancellationToken);
            }
            else if (op.Kind == TelecomOperationKind.SimSwap)
            {
                await _simSwapCompletion.NotifyAndAuditAsync(
                    op,
                    notification.Msisdn ?? lineContext.Msisdn,
                    notification.ActorUserId,
                    cancellationToken);
            }
            else if (op.Kind == TelecomOperationKind.NumberPortability)
            {
                await _changeNumberCompletion.NotifyAndAuditAsync(
                    op,
                    notification.Msisdn ?? lineContext.Msisdn,
                    notification.ActorUserId,
                    cancellationToken);
            }
            else if (op.Kind == TelecomOperationKind.Migration)
            {
                await _migrationCompletion.NotifyAndAuditAsync(
                    op,
                    notification.Msisdn ?? lineContext.Msisdn,
                    notification.ActorUserId,
                    cancellationToken);
            }
            else if (op.Kind == TelecomOperationKind.Termination)
            {
                await _terminationCompletion.NotifyAndAuditAsync(
                    op,
                    notification.Msisdn ?? lineContext.Msisdn,
                    notification.ActorUserId,
                    cancellationToken);
            }
            else if (op.Kind == TelecomOperationKind.TemporarySuspension)
            {
                await _suspensionCompletion.NotifyAndAuditAsync(
                    op,
                    notification.Msisdn ?? lineContext.Msisdn,
                    notification.ActorUserId,
                    cancellationToken);
            }
            else if (op.Kind == TelecomOperationKind.Reconnect)
            {
                await _reconnectCompletion.NotifyAndAuditAsync(
                    op,
                    notification.Msisdn ?? lineContext.Msisdn,
                    notification.ActorUserId,
                    cancellationToken);
            }
        }
        else
        {
            var compensation = await _hlrFailureCompensator.CompensateAsync(
                op,
                lineContext,
                notification.ActorUserId,
                result.Message,
                cancellationToken);

            await _orchestrator.TransitionAsync(
                op,
                TelecomOperationStatus.Failed,
                notification.ActorUserId,
                compensation.MessageAr,
                cancellationToken);

            await _ticketQueue.EnqueueProvisioningFalloutAsync(
                op,
                $"HLR: {result.Message}",
                notification.ActorUserId,
                cancellationToken);

            _logger.LogWarning(
                "HLR failed for operation {OperationId} ({Kind}): {Message}. CBS reversed={CbsReversed}, local={Local}",
                notification.TelecomOperationRequestId,
                notification.Kind,
                result.Message,
                compensation.CbsReversed,
                compensation.LocalBindCompensated);
        }

        _operationRepository.Update(op);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}

public sealed class TelecomOperationSmsHandler : INotificationHandler<TelecomOperationProvisionedNotification>
{
    private readonly ISmsGatewayIntegration _sms;
    private readonly ILogger<TelecomOperationSmsHandler> _logger;

    public TelecomOperationSmsHandler(ISmsGatewayIntegration sms, ILogger<TelecomOperationSmsHandler> logger)
    {
        _sms = sms;
        _logger = logger;
    }

    public async Task Handle(TelecomOperationProvisionedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Kind != TelecomOperationKind.NewActivation || string.IsNullOrEmpty(notification.Msisdn))
        {
            return;
        }

        var body = $"مرحباً بك في سيريتل. تم تفعيل خطك {notification.Msisdn} بنجاح.";
        var result = await _sms.SendAsync(notification.Msisdn, body, cancellationToken);
        _logger.LogInformation("Welcome SMS for {Msisdn}: {Message}", notification.Msisdn, result.Message);
    }
}
