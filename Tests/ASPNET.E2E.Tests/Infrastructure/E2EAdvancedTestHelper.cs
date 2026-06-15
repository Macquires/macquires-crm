using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace ASPNET.E2E.Tests.Infrastructure;

public static class E2EAdvancedTestHelper
{
    public static async Task TriggerOutboxDispatchBatchAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationMessagePublisher>();

        var batch = await db.IntegrationOutboxMessage
            .Where(x => !x.IsDeleted && x.ProcessedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var message in batch)
        {
            await publisher.PublishAsync(
                message.EventType,
                message.PayloadJson,
                message.CorrelationId,
                cancellationToken);

            message.ProcessedAtUtc = DateTime.UtcNow;
            message.AttemptCount++;
        }

        if (batch.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public static async Task<IntegrationOutboxSnapshot> GetOutboxSnapshotAsync(
        IServiceProvider services,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        var rows = await db.IntegrationOutboxMessage.AsNoTracking()
            .Where(m => !m.IsDeleted && m.CorrelationId == correlationId)
            .ToListAsync(cancellationToken);

        return new IntegrationOutboxSnapshot(
            rows.Count,
            rows.Count(m => m.ProcessedAtUtc != null),
            rows.Sum(m => m.AttemptCount));
    }

    public static async Task QuarantineMsisdnAsync(
        IServiceProvider services,
        string msisdnAssetId,
        DateTime quarantineEndsUtc,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var asset = await db.MsisdnAsset.FirstAsync(m => m.Id == msisdnAssetId, cancellationToken);
        asset.TransitionTo(MsisdnPoolStatus.Quarantined);
        asset.QuarantineEndsUtc = quarantineEndsUtc;
        asset.SubscriberProfileId = null;
        asset.ReservedForCustomerId = null;
        asset.ReservedUntilUtc = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task ReleaseMsisdnFromQuarantineAsync(
        IServiceProvider services,
        string msisdnAssetId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var asset = await db.MsisdnAsset.FirstAsync(m => m.Id == msisdnAssetId, cancellationToken);
        asset.QuarantineEndsUtc = DateTime.UtcNow.AddMinutes(-1);
        asset.ReleaseQuarantineIfExpired(DateTime.UtcNow);
        asset.SubscriberProfileId = null;
        asset.PairedIccid = null;
        asset.PairedImsi = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<MsisdnPoolStatus> GetMsisdnPoolStatusAsync(
        IServiceProvider services,
        string msisdnAssetId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await db.MsisdnAsset.AsNoTracking()
            .Where(m => m.Id == msisdnAssetId)
            .Select(m => m.PoolStatus)
            .FirstAsync(cancellationToken);
    }

    public static async Task<string?> GetSubscriberProfileIdForMsisdnAsync(
        IServiceProvider services,
        string msisdnAssetId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await db.MsisdnAsset.AsNoTracking()
            .Where(m => m.Id == msisdnAssetId)
            .Select(m => m.SubscriberProfileId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task SetSimulatorBalanceAsync(
        HttpClient simulatorClient,
        string msisdn,
        decimal balance,
        CancellationToken cancellationToken = default)
    {
        var response = await simulatorClient.PostAsJsonAsync(
            $"/cbs/subscribers/{Uri.EscapeDataString(msisdn)}/adjust-balance",
            new { newBalance = balance, reason = "E2E boundary", idempotencyKey = Guid.NewGuid().ToString("N") },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public sealed record IntegrationOutboxSnapshot(int TotalMessages, int ProcessedCount, int TotalAttempts);
}
