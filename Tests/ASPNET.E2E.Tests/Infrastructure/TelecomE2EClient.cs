using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Enums;

namespace ASPNET.E2E.Tests.Infrastructure;

public sealed class TelecomE2EClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _client;
    private readonly HttpClient _simulatorClient;

    public HttpClient Client => _client;

    public TelecomE2EClient(HttpClient client, HttpClient simulatorClient)
    {
        _client = client;
        _simulatorClient = simulatorClient;
        _client.Timeout = TimeSpan.FromMinutes(5);
    }

    public void UseBearerToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public async Task<string> CreateOperationAsync(object payload, CancellationToken cancellationToken = default)
    {
        var response = await CreateOperationRawAsync(payload, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"CreateTelecomOperation failed ({response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
        var operationId = json.GetProperty("content").GetProperty("data").GetProperty("id").GetString();
        return operationId ?? throw new InvalidOperationException("Create response missing operation id.");
    }

    public Task<HttpResponseMessage> CreateOperationRawAsync(object payload, CancellationToken cancellationToken = default) =>
        _client.PostAsJsonAsync("/api/Telecom/CreateTelecomOperation", payload, cancellationToken);

    public async Task UploadDocumentAsync(string operationId, CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Telecom/UploadTelecomOperationDocument",
            new { id = operationId },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ConfirmOperationResult> ConfirmAsync(string operationId, CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Telecom/ConfirmTelecomOperation",
            new { id = operationId },
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"ConfirmTelecomOperation failed ({response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
        var content = json.GetProperty("content");
        var data = content.GetProperty("data");
        var statusValue = data.GetProperty("status").GetInt32();
        var idempotentReplay = content.TryGetProperty("idempotentReplay", out var replayProp) && replayProp.GetBoolean();

        return new ConfirmOperationResult(
            operationId,
            (TelecomOperationStatus)statusValue,
            idempotentReplay);
    }

    public async Task<HttpResponseMessage> ConfirmRawAsync(string operationId, CancellationToken cancellationToken = default) =>
        await _client.PostAsJsonAsync(
            "/api/Telecom/ConfirmTelecomOperation",
            new { id = operationId },
            cancellationToken);

    public async Task ApproveBackOfficeAsync(string operationId, CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/TelecomBackOffice/ApproveRequest",
            new { operationId },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<HttpResponseMessage> GetOperationDetailRawAsync(string operationId, CancellationToken cancellationToken = default) =>
        await _client.GetAsync($"/api/Telecom/GetTelecomOperationDetail?id={operationId}", cancellationToken);

    public async Task<OperationDetailResult> GetOperationDetailAsync(string operationId, CancellationToken cancellationToken = default)
    {
        var response = await GetOperationDetailRawAsync(operationId, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"GetTelecomOperationDetail failed ({response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
        var data = json.GetProperty("content").GetProperty("data");
        var statusValue = data.GetProperty("status").GetInt32();
        DateTime? scheduledEffective = null;
        if (data.TryGetProperty("scheduledEffectiveDateUtc", out var eff) && eff.ValueKind != JsonValueKind.Null)
        {
            scheduledEffective = eff.GetDateTime();
        }

        return new OperationDetailResult(
            operationId,
            (TelecomOperationStatus)statusValue,
            scheduledEffective);
    }

    public async Task<PaymentDraftResult> CreatePaymentTransactionAsync(
        object payload,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync("/api/Telecom/CreatePaymentTransaction", payload, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"CreatePaymentTransaction failed ({response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
        var content = json.GetProperty("content");
        return new PaymentDraftResult(
            content.GetProperty("paymentId").GetString() ?? "",
            content.GetProperty("number").GetString() ?? "",
            content.TryGetProperty("correlationId", out var corr) ? corr.GetString() : null);
    }

    public async Task<PaymentConfirmResult> ConfirmPaymentTransactionAsync(
        string paymentId,
        string gatewayReference,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Telecom/ConfirmPaymentTransaction",
            new { paymentId, gatewayReference },
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"ConfirmPaymentTransaction failed ({response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
        var data = json.GetProperty("content");
        return new PaymentConfirmResult(
            data.GetProperty("success").GetBoolean(),
            data.TryGetProperty("newBalance", out var bal) && bal.ValueKind != JsonValueKind.Null
                ? bal.GetDecimal()
                : null,
            data.TryGetProperty("paymentNumber", out var num) ? num.GetString() : null,
            data.TryGetProperty("receiptNumber", out var rcpt) ? rcpt.GetString() : null);
    }

    public async Task ReserveMsisdnAsync(string msisdnAssetId, string customerId, CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Telecom/ReserveMsisdnForCustomer",
            new { msisdnAssetId, customerId },
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"ReserveMsisdn failed ({response.StatusCode}): {body}");
        }
    }

    public async Task SetSimulatorChaosAsync(
        bool enabled,
        int timeoutProbability,
        CancellationToken cancellationToken = default,
        int responseDelayMs = 0,
        bool corruptPayload = false)
    {
        var response = await _simulatorClient.PostAsJsonAsync(
            "/admin/chaos",
            new
            {
                enabled,
                timeoutProbability,
                rateLimitProbability = 0,
                responseDelayMs,
                corruptPayload,
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResetSimulatorChaosAsync(CancellationToken cancellationToken = default) =>
        await SetSimulatorChaosAsync(enabled: false, timeoutProbability: 0, cancellationToken);

    public sealed record ConfirmOperationResult(
        string OperationId,
        TelecomOperationStatus Status,
        bool IdempotentReplay);

    public sealed record OperationDetailResult(
        string OperationId,
        TelecomOperationStatus Status,
        DateTime? ScheduledEffectiveDateUtc);

    public sealed record PaymentDraftResult(string PaymentId, string Number, string? CorrelationId);

    public sealed record PaymentConfirmResult(
        bool Success,
        decimal? NewBalance,
        string? PaymentNumber,
        string? ReceiptNumber);
}
