using System.Text.Json;
using Application.Common.Events;
using Application.Common.Integrations;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations.Outbox;

public sealed class IntegrationOutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrationOutboxDispatcherHostedService> _logger;

    public IntegrationOutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<IntegrationOutboxDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var batch = await db.IntegrationOutboxMessage
            .Where(x => !x.IsDeleted && x.ProcessedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var message in batch)
        {
            try
            {
                if (string.Equals(message.EventType, nameof(TelecomOperationProvisionedNotification), StringComparison.Ordinal))
                {
                    var notification = JsonSerializer.Deserialize<TelecomOperationProvisionedNotification>(message.PayloadJson);
                    if (notification != null)
                    {
                        await publisher.Publish(notification, cancellationToken);
                    }
                }

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
    }
}
