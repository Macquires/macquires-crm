using Application.Common.CQS.Queries;
using Application.Common.Events;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
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

    public TelecomOperationHlrHandler(
        INetworkProvisioningService network,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ITelecomOperationOrchestrator orchestrator,
        ITelecomHlrFailureCompensator hlrFailureCompensator,
        IQueryContext query,
        IUnitOfWork unitOfWork,
        ILogger<TelecomOperationHlrHandler> logger)
    {
        _network = network;
        _operationRepository = operationRepository;
        _orchestrator = orchestrator;
        _hlrFailureCompensator = hlrFailureCompensator;
        _query = query;
        _unitOfWork = unitOfWork;
        _logger = logger;
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
            op.Status = TelecomOperationStatus.PendingExternal;
            op.UpdatedById = notification.ActorUserId;
        }
        else if (result.Success)
        {
            op.Status = TelecomOperationStatus.Completed;
            op.UpdatedById = notification.ActorUserId;
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
