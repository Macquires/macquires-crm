using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Infrastructure.TelecomIntegrations;

/// <summary>Demo Huawei CBS bridge: Polly retries, per-attempt <see cref="BillingIntegrationLog"/> rows.</summary>
public sealed class HuaweiCbsBillingIntegration : IBillingSystemIntegration
{
    private readonly ICommandRepository<BillingIntegrationLog> _logRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<TelecomBillingOptions> _options;
    private readonly ILogger<HuaweiCbsBillingIntegration> _logger;

    public HuaweiCbsBillingIntegration(
        ICommandRepository<BillingIntegrationLog> logRepository,
        IUnitOfWork unitOfWork,
        IOptions<TelecomBillingOptions> options,
        ILogger<HuaweiCbsBillingIntegration> logger)
    {
        _logRepository = logRepository;
        _unitOfWork = unitOfWork;
        _options = options;
        _logger = logger;
    }

    public async Task<BillingProvisionResult> ProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default)
    {
        var opt = _options.Value;
        var attempt = 0;

        var policy = Policy
            .Handle<InvalidOperationException>()
            .WaitAndRetryAsync(
                opt.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(opt.RetryBaseDelayMs * retryAttempt),
                onRetry: (ex, ts, retryCount, ctx) =>
                {
                    _logger.LogWarning(ex, "Huawei CBS mock retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds);
                });

        try
        {
            return await policy.ExecuteAsync(async () =>
            {
                attempt++;
                var failUntil = Math.Max(0, opt.FailAttemptsBeforeSuccess);

                if (attempt <= failUntil)
                {
                    await WriteLogAsync(request.OperationId, attempt, false, "Huawei CBS timeout (simulated)", opt.IntegrationTarget, cancellationToken);
                    throw new InvalidOperationException("Huawei CBS timeout (simulated)");
                }

                await WriteLogAsync(request.OperationId, attempt, true, $"Provisioned {request.Kind} for {request.OperationNumber}", opt.IntegrationTarget, cancellationToken);
                return new BillingProvisionResult(true, $"Huawei CBS OK (attempt {attempt})");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Huawei CBS mock exhausted retries for {OperationNumber}", request.OperationNumber);
            return new BillingProvisionResult(false, ex.Message);
        }
    }

    private async Task WriteLogAsync(
        string telecomOperationRequestId,
        int attemptNumber,
        bool success,
        string message,
        string? integrationTarget,
        CancellationToken cancellationToken)
    {
        var row = new BillingIntegrationLog
        {
            TelecomOperationRequestId = telecomOperationRequestId,
            AttemptNumber = attemptNumber,
            Success = success,
            Message = message,
            IntegrationTarget = integrationTarget
        };

        await _logRepository.CreateAsync(row, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
