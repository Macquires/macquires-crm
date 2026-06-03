using Application.Common.CQS.Queries;
using Application.Common.Telecom.Reconnect;
using Application.Features.TelecomManager.Commands;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>Daily scan for SUS operations with AutoReconnectEnabled nearing SuspensionEndDateUtc.</summary>
public sealed class SuspensionAutoReconnectHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SuspensionAutoReconnectHostedService> _logger;

    public SuspensionAutoReconnectHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SuspensionAutoReconnectHostedService> logger)
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
                await ProcessDueReconnectsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Suspension auto-reconnect worker failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessDueReconnectsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var horizon = DateTime.UtcNow.AddDays(1);

        var due = await query.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.TemporarySuspension
                        && o.Status == TelecomOperationStatus.Completed
                        && o.AutoReconnectEnabled
                        && o.SuspensionEndDateUtc != null
                        && o.SuspensionEndDateUtc <= horizon)
            .Select(o => new
            {
                o.Id,
                o.Number,
                o.SubscriberProfileId,
                o.MsisdnAssetId,
            })
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var sus in due)
        {
            if (string.IsNullOrEmpty(sus.MsisdnAssetId))
            {
                continue;
            }

            var hasOpen = await query.TelecomOperationRequest.AsNoTracking()
                .Where(o => !o.IsDeleted
                            && o.MsisdnAssetId == sus.MsisdnAssetId
                            && (o.Kind == TelecomOperationKind.Reconnect
                                || o.Kind == TelecomOperationKind.TemporarySuspension)
                            && o.Status != TelecomOperationStatus.Completed
                            && o.Status != TelecomOperationStatus.Failed)
                .AnyAsync(cancellationToken);

            if (hasOpen)
            {
                continue;
            }

            var profileActive = await query.SubscriberProfile.AsNoTracking()
                .Where(p => !p.IsDeleted && p.Id == sus.SubscriberProfileId)
                .Select(p => p.OperationalStatus)
                .FirstOrDefaultAsync(cancellationToken);

            if (profileActive == SubscriberOperationalStatus.Active)
            {
                continue;
            }

            try
            {
                await mediator.Send(
                    new CreateTelecomOperationRequest
                    {
                        Kind = TelecomOperationKind.Reconnect,
                        SubscriberProfileId = sus.SubscriberProfileId,
                        MsisdnAssetId = sus.MsisdnAssetId,
                        ReconnectReason = $"إعادة تفعيل تلقائية بعد انتهاء {sus.Number}",
                        ClearanceType = ReconnectWellKnown.Customer,
                        SourceSuspensionOperationId = sus.Id,
                        CreatedById = "system-auto-reconnect",
                    },
                    cancellationToken);

                _logger.LogInformation("Auto-reconnect draft RCN created for suspension {SuspensionNumber}", sus.Number);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-reconnect skipped for {SuspensionNumber}", sus.Number);
            }
        }
    }
}
