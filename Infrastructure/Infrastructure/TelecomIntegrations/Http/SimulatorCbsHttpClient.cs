using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.TelecomIntegrations.Http;

public sealed class SimulatorCbsHttpClient
{
    private readonly HttpClient _http;
    private readonly TelecomHttpIntegrationOptions _options;
    private readonly ILogger<SimulatorCbsHttpClient> _logger;

    public SimulatorCbsHttpClient(
        HttpClient http,
        IOptions<TelecomHttpIntegrationOptions> options,
        ILogger<SimulatorCbsHttpClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(_options.SimulatorBaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<BillingProvisionResult> ProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken)
    {
        var msisdn = request.Msisdn ?? "unknown";
        var response = await _http.PostAsJsonAsync(
            $"cbs/subscribers/{Uri.EscapeDataString(msisdn)}/provision",
            new { initialDeposit = request.InitialDeposit },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Simulator CBS provision failed {Status}: {Body}", response.StatusCode, body);
            return new BillingProvisionResult(false, body);
        }

        return new BillingProvisionResult(true, $"Simulator CBS provision OK for {msisdn}");
    }

    public async Task<BillingProvisionResult> ReverseAsync(BillingProvisionRequest request, CancellationToken cancellationToken)
    {
        var msisdn = request.Msisdn ?? "unknown";
        var response = await _http.PostAsync(
            $"cbs/subscribers/{Uri.EscapeDataString(msisdn)}/reverse",
            null,
            cancellationToken);

        return response.IsSuccessStatusCode
            ? new BillingProvisionResult(true, $"Simulator CBS reverse OK for {msisdn}")
            : new BillingProvisionResult(false, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task AdjustBalanceAsync(
        string msisdn,
        decimal newBalance,
        string? reason,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync(
            $"cbs/subscribers/{Uri.EscapeDataString(msisdn)}/adjust-balance",
            new { newBalance, reason, idempotencyKey },
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task<decimal> GetBalanceAsync(string msisdn, CancellationToken cancellationToken)
    {
        var response = await _http.GetAsync($"cbs/subscribers/{Uri.EscapeDataString(msisdn)}/balance", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return doc.RootElement.GetProperty("balance").GetDecimal();
    }

    public async Task<BillingRechargeResult> RechargeAsync(
        string msisdn,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync(
            $"cbs/subscribers/{Uri.EscapeDataString(msisdn)}/recharge",
            new { amount },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Simulator CBS recharge failed {Status}: {Body}", response.StatusCode, body);
            return new BillingRechargeResult(false, body);
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var balance = doc.RootElement.GetProperty("balance").GetDecimal();
        return new BillingRechargeResult(true, $"Simulator CBS recharge OK for {msisdn}", balance);
    }
}
