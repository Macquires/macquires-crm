using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Common.Telecom.ChangeNumber;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationCreate;

public sealed class OperationCreatePostCreateService : IOperationCreatePostCreateService
{
    private readonly IOperationCreateStrategyRegistry _strategies;
    private readonly ITechnicalTicketQueueIngestionService _ticketQueue;
    private readonly ICommandRepository<DeviceInventory> _deviceInventoryRepository;
    private readonly IMnpPortabilityGateway _mnpGateway;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OperationCreatePostCreateService(
        IOperationCreateStrategyRegistry strategies,
        ITechnicalTicketQueueIngestionService ticketQueue,
        ICommandRepository<DeviceInventory> deviceInventoryRepository,
        IMnpPortabilityGateway mnpGateway,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IUnitOfWork unitOfWork)
    {
        _strategies = strategies;
        _ticketQueue = ticketQueue;
        _deviceInventoryRepository = deviceInventoryRepository;
        _mnpGateway = mnpGateway;
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task RunAsync(
        TelecomOperationRequest entity,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        var flags = _strategies.Resolve(entity.Kind).PostCreateFlags;

        if (flags.HasFlag(OperationCreatePostCreateFlags.ReserveDeviceInventory)
            && entity.Kind == TelecomOperationKind.DeviceSale
            && !string.IsNullOrEmpty(entity.DeviceInventoryId))
        {
            var device = await _deviceInventoryRepository.GetAsync(entity.DeviceInventoryId, cancellationToken);
            if (device != null && device.Status == DeviceInventoryStatus.Available)
            {
                device.ReserveForOperation(entity.Id);
                device.UpdatedById = actorUserId;
                _deviceInventoryRepository.Update(device);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }

        if (flags.HasFlag(OperationCreatePostCreateFlags.EnqueueTechnicalTicket))
        {
            await _ticketQueue.EnqueueFromTelecomOperationAsync(entity, actorUserId, cancellationToken);
        }

        if (entity.Kind == TelecomOperationKind.NumberPortability
            && ChangeNumberWellKnown.IsPortInMode(entity.NumberChangeMode)
            && !string.IsNullOrWhiteSpace(entity.PortInMsisdn)
            && string.IsNullOrWhiteSpace(entity.ExternalCorrelationId))
        {
            var msisdn = await _operationRepository.GetQuery()
                .Where(o => o.Id == entity.Id)
                .Select(o => o.MsisdnAsset!.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);

            var mnpResult = await _mnpGateway.SubmitPortInOrderAsync(
                new MnpPortInOrderRequest(
                    entity.Id,
                    entity.Number,
                    msisdn ?? string.Empty,
                    entity.PortInMsisdn!,
                    entity.DonorOperatorCode ?? string.Empty,
                    entity.AgencyReference,
                    entity.CorrelationId),
                cancellationToken);

            if (mnpResult.Success && !string.IsNullOrWhiteSpace(mnpResult.ExternalCorrelationId))
            {
                var tracked = await _operationRepository.GetAsync(entity.Id, cancellationToken);
                if (tracked != null)
                {
                    tracked.ExternalCorrelationId = mnpResult.ExternalCorrelationId;
                    tracked.UpdatedById = actorUserId;
                    _operationRepository.Update(tracked);
                    await _unitOfWork.SaveAsync(cancellationToken);
                }
            }
        }
    }
}
