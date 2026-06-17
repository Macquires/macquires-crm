using Application.Common.Distributed;
using Application.Common.Integrations;
using Application.Common.Security;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations.Outbox;

public sealed class IntegrationOutboxDispatcherHostedService : BackgroundService
{
    private const string DispatchLockKey = "integration-outbox-dispatch";
    private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrationOutboxDispatcherHostedService> _logger;
    private readonly TimeSpan _dispatchInterval;

    public IntegrationOutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<IntegrationOutboxDispatcherHostedService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var seconds = configuration.GetValue("IntegrationOutbox:DispatchIntervalSeconds", 2);
        _dispatchInterval = TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 120));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher batch failed.");
            }

            await Task.Delay(_dispatchInterval, stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        using (scope.ServiceProvider.GetRequiredService<ISystemExecutionGate>().Enter())
        {
            var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();
            await using var lockHandle = await distributedLock.TryAcquireAsync(DispatchLockKey, LockTtl, cancellationToken);
            if (lockHandle is null)
            {
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationMessagePublisher>();
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                var batch = await db.IntegrationOutboxMessage
                    .FromSqlInterpolated($@"
                        SELECT *
                        FROM IntegrationOutboxMessage WITH (UPDLOCK, READPAST, ROWLOCK)
                        WHERE ProcessedAtUtc IS NULL AND IsDeleted = 0
                        ORDER BY OccurredAtUtc
                        OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY")
                    .ToListAsync(cancellationToken);

                foreach (var message in batch)
                {
                    try
                    {
                        await publisher.PublishAsync(
                            message.EventType,
                            message.PayloadJson,
                            message.CorrelationId,
                            cancellationToken);

                        message.ProcessedAtUtc = DateTime.UtcNow;
                        message.AttemptCount++;
                    }
                    catch (Exception ex)
                    {
                        message.AttemptCount++;
                        message.LastError = ex.Message;
                        _logger.LogWarning(ex, "Failed to dispatch outbox message {Id}", message.Id);
                    }
                }

                if (batch.Count > 0)
                {
                    await db.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            });
        }
    }
}
