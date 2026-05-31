using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

public sealed class PendingExternalSyncService : IPendingExternalSyncService
{
    private const int MaxBatch = 500;

    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IBillingSystemIntegration _billing;
    private readonly INetworkProvisioningService _network;
    private readonly ITelecomHlrFailureCompensator _hlrFailureCompensator;
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PendingExternalSyncService> _logger;

    public PendingExternalSyncService(
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IBillingSystemIntegration billing,
        INetworkProvisioningService network,
        ITelecomHlrFailureCompensator hlrFailureCompensator,
        IQueryContext query,
        IUnitOfWork unitOfWork,
        ILogger<PendingExternalSyncService> logger)
    {
        _operationRepository = operationRepository;
        _billing = billing;
        _network = network;
        _hlrFailureCompensator = hlrFailureCompensator;
        _query = query;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PendingExternalSyncResult> FlushAsync(string? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var ops = await _operationRepository.GetQuery()
            .Where(o => !o.IsDeleted && o.Status == TelecomOperationStatus.PendingExternal)
            .OrderBy(o => o.UpdatedAtUtc ?? o.CreatedAtUtc)
            .Take(MaxBatch)
            .ToListAsync(cancellationToken);

        var errors = new List<string>();
        var succeeded = 0;
        var stillPending = 0;
        var failed = 0;

        foreach (var op in ops)
        {
            try
            {
                var lineContext = await TelecomProvisionRequestBuilder.ResolveLineContextAsync(_query, op, cancellationToken);
                var billingRequest = TelecomProvisionRequestBuilder.ToBillingRequest(op, lineContext);
                var billing = await _billing.ProvisionAsync(billingRequest, cancellationToken);
                if (!billing.Success)
                {
                    failed++;
                    errors.Add($"{op.Number}: CBS — {billing.Message}");
                    continue;
                }

                if (billing.QueuedForSync)
                {
                    stillPending++;
                    continue;
                }

                var netRequest = TelecomProvisionRequestBuilder.ToNetworkRequest(op, lineContext);
                var net = await _network.ProvisionAsync(netRequest, cancellationToken);
                if (net.DeferRetry || net.QueuedForSync)
                {
                    stillPending++;
                    continue;
                }

                if (net.Success)
                {
                    op.Status = TelecomOperationStatus.Completed;
                    op.UpdatedById = actorUserId ?? op.UpdatedById;
                    _operationRepository.Update(op);
                    succeeded++;
                }
                else
                {
                    await _hlrFailureCompensator.CompensateAsync(
                        op,
                        lineContext,
                        actorUserId,
                        net.Message,
                        cancellationToken);
                    op.Status = TelecomOperationStatus.Failed;
                    op.UpdatedById = actorUserId ?? op.UpdatedById;
                    _operationRepository.Update(op);
                    failed++;
                    errors.Add($"{op.Number}: HLR — {net.Message}");
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{op.Number}: {ex.Message}");
                _logger.LogWarning(ex, "PendingExternal sync failed for {OperationId}", op.Id);
            }
        }

        if (succeeded > 0 || failed > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new PendingExternalSyncResult(ops.Count, succeeded, stillPending, failed, errors);
    }
}
