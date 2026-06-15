using System.Net.Http.Json;
using Application.Common.Integrations;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations.Http;

public sealed class SimulatorSmsHttpClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SimulatorSmsHttpClient> _logger;

    public SimulatorSmsHttpClient(HttpClient http, ILogger<SimulatorSmsHttpClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<BillingProvisionResult> SendAsync(
        string phoneNumber,
        string body,
        CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync(
            "sms/send",
            new { to = phoneNumber, body },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Simulator SMS failed {Status}: {Body}", response.StatusCode, error);
            return new BillingProvisionResult(false, error);
        }

        return new BillingProvisionResult(true, $"SMS sent via simulator → {phoneNumber}");
    }
}
