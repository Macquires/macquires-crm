using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ASPNET.E2E.Tests.Infrastructure;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(ActivationE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class ActivationApiIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly TelecomE2EFixture _fixture;

    public ActivationApiIntegrationTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task Activation_happy_path_writes_outbox_and_reaches_simulator()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        var ct = cts.Token;

        var client = _fixture.AppFactory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
        client.Timeout = TimeSpan.FromMinutes(5);

        var seed = await E2ETestDataHelper.ResolveActivationSeedAsync(_fixture.AppFactory.Services, ct);
        var token = await LoginAsync(client, ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var operationId = await CreateActivationAsync(client, seed, ct);
        var correlationId = await GetCorrelationIdAsync(operationId, ct);

        var confirm = await ConfirmActivationAsync(client, operationId, ct);
        Assert.NotNull(confirm);
        Assert.True(
            confirm.Status is TelecomOperationStatus.Provisioning
                or TelecomOperationStatus.Completed
                or TelecomOperationStatus.PendingExternal,
            $"Unexpected immediate confirm status: {confirm.Status}");

        await E2ETestDataHelper.WaitForOutboxProcessedAsync(
            _fixture.AppFactory.Services,
            correlationId,
            TimeSpan.FromSeconds(90),
            ct);

        var finalStatus = await E2ETestDataHelper.WaitForOperationStatusAsync(
            _fixture.AppFactory.Services,
            operationId,
            s => s is TelecomOperationStatus.Completed or TelecomOperationStatus.Failed,
            TimeSpan.FromSeconds(60),
            ct);

        Assert.Equal(TelecomOperationStatus.Completed, finalStatus);

        using (var scope = _fixture.AppFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var outbox = await db.IntegrationOutboxMessage.AsNoTracking()
                .Where(m => !m.IsDeleted && m.CorrelationId == correlationId)
                .ToListAsync(ct);
            Assert.NotEmpty(outbox);
            Assert.All(outbox, m => Assert.NotNull(m.ProcessedAtUtc));
            Assert.Contains(outbox, m => m.EventType == nameof(Application.Common.Events.TelecomOperationProvisionedNotification));

            var integrationLogs = await db.TelecomIntegrationLog.AsNoTracking()
                .Where(l => !l.IsDeleted && l.Msisdn == seed.Msisdn)
                .ToListAsync(ct);
            Assert.Contains(integrationLogs, l =>
                l.IntegrationSystem == TelecomIntegrationSystem.Huawei_CBS && l.IsSuccess);
            Assert.Contains(integrationLogs, l =>
                l.IntegrationSystem == TelecomIntegrationSystem.Huawei_HLR && l.IsSuccess);

            var msisdn = await db.MsisdnAsset.AsNoTracking()
                .FirstAsync(m => m.Id == seed.MsisdnAssetId, ct);
            Assert.Equal(MsisdnPoolStatus.Active, msisdn.PoolStatus);
            Assert.Equal(seed.SubscriberProfileId, msisdn.SubscriberProfileId);
        }

        var hlrStatus = await _fixture.SimulatorClient.GetAsync($"/hlr/subscribers/{seed.Msisdn}/status", ct);
        Assert.Equal(HttpStatusCode.OK, hlrStatus.StatusCode);
        var hlrBody = await hlrStatus.Content.ReadAsStringAsync(ct);
        Assert.Contains("ACTIVE", hlrBody, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Activation_hlr_timeout_returns_clean_failure_and_compensation()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        var ct = cts.Token;

        try
        {
            var client = _fixture.AppFactory.CreateClient(new()
            {
                AllowAutoRedirect = false,
                HandleCookies = false,
            });
            client.Timeout = TimeSpan.FromMinutes(5);

            var baseline = await E2ETestDataHelper.ResolveActivationSeedAsync(_fixture.AppFactory.Services, ct);
            var seed = await E2ETestDataHelper.ResolveAlternateActivationSeedAsync(
                _fixture.AppFactory.Services,
                baseline.MsisdnAssetId,
                ct);

            var token = await LoginAsync(client, ct);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var operationId = await CreateActivationAsync(client, seed, ct);
            var correlationId = await GetCorrelationIdAsync(operationId, ct);

            var confirm = await ConfirmActivationAsync(client, operationId, ct);
            Assert.NotNull(confirm);
            Assert.True(
                confirm.Status is TelecomOperationStatus.Provisioning
                    or TelecomOperationStatus.Completed
                    or TelecomOperationStatus.PendingExternal,
                $"Unexpected confirm status before HLR chaos: {confirm.Status}");

            await E2ETestDataHelper.WaitForPendingOutboxAsync(
                _fixture.AppFactory.Services,
                correlationId,
                TimeSpan.FromSeconds(10),
                ct);

            await SetSimulatorChaosAsync(enabled: true, timeoutProbability: 100, ct);

            await E2ETestDataHelper.WaitForOutboxProcessedAsync(
                _fixture.AppFactory.Services,
                correlationId,
                TimeSpan.FromSeconds(60),
                ct);

            var finalStatus = await E2ETestDataHelper.WaitForOperationStatusAsync(
                _fixture.AppFactory.Services,
                operationId,
                s => s is TelecomOperationStatus.Failed
                    or TelecomOperationStatus.ProvisioningError
                    or TelecomOperationStatus.PendingExternal,
                TimeSpan.FromSeconds(60),
                ct);

            Assert.True(
                finalStatus is TelecomOperationStatus.Failed
                    or TelecomOperationStatus.ProvisioningError,
                $"Expected failure compensation status, got {finalStatus}");

            using var scope = _fixture.AppFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var hlrLogs = await db.TelecomIntegrationLog.AsNoTracking()
                .Where(l => !l.IsDeleted
                            && l.Msisdn == seed.Msisdn
                            && l.IntegrationSystem == TelecomIntegrationSystem.Huawei_HLR)
                .ToListAsync(ct);
            Assert.Contains(hlrLogs, l => !l.IsSuccess);

            var cbsLogs = await db.TelecomIntegrationLog.AsNoTracking()
                .Where(l => !l.IsDeleted
                            && l.Msisdn == seed.Msisdn
                            && l.IntegrationSystem == TelecomIntegrationSystem.Huawei_CBS)
                .ToListAsync(ct);
            Assert.Contains(cbsLogs, l => l.IsSuccess);
        }
        finally
        {
            await SetSimulatorChaosAsync(enabled: false, timeoutProbability: 0, CancellationToken.None);
        }
    }

    private async Task<string> LoginAsync(HttpClient client, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync(
            "/api/Security/Login",
            new { email = "admin@root.com", password = "123456" },
            ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var token = json.GetProperty("content").GetProperty("data").GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private async Task<string> CreateActivationAsync(
        HttpClient client,
        ActivationSeedBundle seed,
        CancellationToken ct)
    {
        var payload = new
        {
            kind = (int)TelecomOperationKind.NewActivation,
            subscriberProfileId = seed.SubscriberProfileId,
            msisdnAssetId = seed.MsisdnAssetId,
            productOfferingId = seed.ProductOfferingId,
            simInventoryId = seed.SimInventoryId,
            kycDocumentReferenceId = seed.KycDocumentReferenceId,
            activationChannel = (int)ActivationChannel.Showroom,
        };

        var response = await client.PostAsJsonAsync("/api/Telecom/CreateTelecomOperation", payload, ct);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            Assert.Fail($"Create failed ({response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var operationId = json.GetProperty("content").GetProperty("data").GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(operationId));
        return operationId!;
    }

    private async Task<OperationSnapshot?> ConfirmActivationAsync(
        HttpClient client,
        string operationId,
        CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync(
            "/api/Telecom/ConfirmTelecomOperation",
            new { id = operationId },
            ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var data = json.GetProperty("content").GetProperty("data");
        var statusValue = data.GetProperty("status").GetInt32();
        return new OperationSnapshot(operationId, (TelecomOperationStatus)statusValue);
    }

    private async Task<string> GetCorrelationIdAsync(string operationId, CancellationToken ct)
    {
        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var correlationId = await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => o.Id == operationId)
            .Select(o => o.CorrelationId)
            .FirstAsync(ct);
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        return correlationId!;
    }

    private async Task SetSimulatorChaosAsync(bool enabled, int timeoutProbability, CancellationToken ct)
    {
        var response = await _fixture.SimulatorClient.PostAsJsonAsync(
            "/admin/chaos",
            new { enabled, timeoutProbability, rateLimitProbability = 0 },
            ct);
        response.EnsureSuccessStatusCode();
    }

    private sealed record OperationSnapshot(string Id, TelecomOperationStatus Status);
}
