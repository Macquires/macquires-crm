using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.TelecomIntegrations.Http;

public sealed class SimulatorHlrHttpClient
{
    private readonly HttpClient _http;
    private readonly TelecomHttpIntegrationOptions _options;
    private readonly ILogger<SimulatorHlrHttpClient> _logger;

    public SimulatorHlrHttpClient(
        HttpClient http,
        IOptions<TelecomHttpIntegrationOptions> options,
        ILogger<SimulatorHlrHttpClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(_options.SimulatorBaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<NetworkProvisionResult> ProvisionAsync(NetworkProvisionRequest request, CancellationToken cancellationToken)
    {
        var msisdn = request.Msisdn ?? "unknown";
        var response = await _http.PostAsJsonAsync(
            $"hlr/subscribers/{Uri.EscapeDataString(msisdn)}/provision",
            new { command = request.Kind.ToString(), imsi = request.Imsi, iccid = request.Iccid },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Simulator HLR provision failed for {Msisdn}: {Status}", msisdn, response.StatusCode);
            return new NetworkProvisionResult(false, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("success", out var successProp)
                || !successProp.GetBoolean())
            {
                return new NetworkProvisionResult(false, $"Simulator HLR rejected provision for {msisdn}.");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Simulator HLR malformed payload for {Msisdn}", msisdn);
            return new NetworkProvisionResult(false, $"Malformed HLR response: {ex.Message}");
        }

        return new NetworkProvisionResult(true, "Simulator HLR provision OK");
    }

    public async Task SuspendSubscriberAsync(string msisdn, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync(
            $"hlr/subscribers/{Uri.EscapeDataString(msisdn)}/provision",
            new { command = "SUSPEND", imsi = (string?)null, iccid = (string?)null },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<HlrLiveStatusResult> QueryLiveStatusAsync(string msisdn, string? crmStatus, CancellationToken cancellationToken)
    {
        var response = await _http.GetAsync($"hlr/subscribers/{Uri.EscapeDataString(msisdn)}/status", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var hlrState = doc.RootElement.GetProperty("hlrSubscriberState").GetString() ?? "ACTIVE";
        var isOnline = doc.RootElement.GetProperty("isOnline").GetBoolean();
        return new HlrLiveStatusResult(
            true,
            "HLR live query OK (simulator)",
            isOnline,
            "Damascus-GMSC-01",
            "417011234567890",
            hlrState,
            !StatesAligned(crmStatus, hlrState),
            crmStatus);
    }

    private static bool StatesAligned(string? crm, string hlr)
    {
        var crmNorm = (crm ?? string.Empty).Trim();
        var hlrNorm = hlr.Trim().ToUpperInvariant();

        if (string.Equals(crmNorm, "Active", StringComparison.OrdinalIgnoreCase) || crmNorm == "1")
        {
            return hlrNorm == "ACTIVE";
        }

        if (string.Equals(crmNorm, "Suspended", StringComparison.OrdinalIgnoreCase)
            || string.Equals(crmNorm, "SuspendedInbound", StringComparison.OrdinalIgnoreCase)
            || string.Equals(crmNorm, "SuspendedOutbound", StringComparison.OrdinalIgnoreCase))
        {
            return hlrNorm is "SUSPENDED" or "INACTIVE";
        }

        if (string.Equals(crmNorm, "Terminated", StringComparison.OrdinalIgnoreCase)
            || string.Equals(crmNorm, "Deactivated", StringComparison.OrdinalIgnoreCase))
        {
            return hlrNorm is "INACTIVE" or "NOT_PROVISIONED";
        }

        return true;
    }
}
