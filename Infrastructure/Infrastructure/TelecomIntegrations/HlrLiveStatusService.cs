using System.Net.Http;
using System.Net.Sockets;
using Application.Common.Integrations;
using Application.Common.Telecom;
using Infrastructure.TelecomIntegrations.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace Infrastructure.TelecomIntegrations;

public sealed class HlrLiveStatusService : IHLRLiveStatusService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<HlrLiveStatusService> _logger;
    private readonly SimulatorHlrHttpClient? _simulator;
    private readonly bool _useHttp;
    private const string CachePrefix = "HLR_STATE_";

    public HlrLiveStatusService(
        IDistributedCache cache,
        ILogger<HlrLiveStatusService> logger,
        IOptions<TelecomHttpIntegrationOptions> httpOptions,
        SimulatorHlrHttpClient? simulator = null)
    {
        _cache = cache;
        _logger = logger;
        _simulator = simulator;
        _useHttp = string.Equals(httpOptions.Value.Mode, "Http", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<HlrLiveStatusResult> QueryLiveStatusAsync(
        string msisdn,
        string? crmOperationalStatus,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HLR live query for {Msisdn}", msisdn);

        var forcedState = await TryGetCachedHlrStateAsync(msisdn, cancellationToken);
        if (!string.IsNullOrEmpty(forcedState))
        {
            return BuildResult(msisdn, forcedState, crmOperationalStatus, "HLR live query OK (reprovisioned/cached)");
        }

        // Revenue leakage showcase — only before HLR remediation (no cached override yet).
        if (TelecomDemoBaselines.IsDebtShowcaseMsisdn(msisdn)
            && IsCrmSuspended(crmOperationalStatus ?? string.Empty))
        {
            return BuildResult(msisdn, "ACTIVE", crmOperationalStatus, "HLR live query OK (Revenue Leakage Demo)");
        }

        // Paid reconnect completed — CRM Active + HLR ACTIVE (hide desync / leakage UI).
        if (TelecomDemoBaselines.IsDebtShowcaseMsisdn(msisdn)
            && IsCrmActive(crmOperationalStatus ?? string.Empty))
        {
            return BuildResult(msisdn, "ACTIVE", crmOperationalStatus, "HLR live query OK (debt showcase reconnected)");
        }

        if (_useHttp && _simulator != null)
        {
            try
            {
                return await _simulator.QueryLiveStatusAsync(msisdn, crmOperationalStatus, cancellationToken);
            }
            catch (Exception ex) when (IsSimulatorUnreachable(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Network Simulator unreachable for HLR on {Msisdn}; using in-process demo fallback.",
                    msisdn);
            }
        }

        return await ResolveMockLiveStatusAsync(msisdn, crmOperationalStatus, cancellationToken);
    }

    private async Task<HlrLiveStatusResult> ResolveMockLiveStatusAsync(
        string msisdn,
        string? crmOperationalStatus,
        CancellationToken cancellationToken)
    {
        // Revenue leakage demo: billing-suspended debt line while HLR still serves until back-office clearance.
        if (TelecomDemoBaselines.IsDebtShowcaseMsisdn(msisdn)
            && IsCrmSuspended(crmOperationalStatus ?? string.Empty))
        {
            return BuildResult(msisdn, "ACTIVE", crmOperationalStatus, "HLR live query OK (Revenue Leakage Demo)");
        }

        // After paid reconnect — CRM Active and HLR must read aligned (no remediation banner).
        if (TelecomDemoBaselines.IsDebtShowcaseMsisdn(msisdn)
            && IsCrmActive(crmOperationalStatus ?? string.Empty))
        {
            return BuildResult(msisdn, "ACTIVE", crmOperationalStatus, "HLR live query OK (debt showcase reconnected)");
        }

        // Special case: Fraud demo line must be NOT_PROVISIONED for VAL-09-03 scenario
        if (msisdn == TelecomDemoMsisdn.ReconnectFraudDemo || msisdn == TelecomDemoMsisdn.ShowcaseNotProvisioned)
        {
            return BuildResult(msisdn, "NOT_PROVISIONED", crmOperationalStatus, "HLR live query OK (demo override)");
        }

        // Special case: Operational suspension line must be SUSPENDED for VAL-09-04 scenario
        if (msisdn == TelecomDemoMsisdn.ShowcaseOperationalSuspended)
        {
            return BuildResult(msisdn, "SUSPENDED", crmOperationalStatus, "HLR live query OK (demo override)");
        }

        // If CRM status is provided, we can be deterministic based on it
        if (!string.IsNullOrWhiteSpace(crmOperationalStatus))
        {
            var hlrState = crmOperationalStatus switch
            {
                "Active" or "1" => "ACTIVE",
                "Suspended" or "SuspendedInbound" or "SuspendedOutbound" => "SUSPENDED",
                "Terminated" or "Deactivated" => "INACTIVE",
                _ => ResolveMockHlrState(msisdn.Length > 0 ? msisdn[^1] - '0' : 0)
            };
            return BuildResult(msisdn, hlrState, crmOperationalStatus, "HLR live query OK (deterministic)");
        }

        var lastDigit = msisdn.Length > 0 ? msisdn[^1] - '0' : 0;
        var fallbackState = ResolveMockHlrState(lastDigit);
        return BuildResult(msisdn, fallbackState, crmOperationalStatus, "HLR live query OK (fallback)");
    }

    private static bool IsSimulatorUnreachable(Exception ex)
    {
        if (ex is TimeoutRejectedException)
        {
            return true;
        }

        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is HttpRequestException or SocketException)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string?> TryGetCachedHlrStateAsync(string msisdn, CancellationToken cancellationToken)
    {
        try
        {
            return await _cache.GetStringAsync($"{CachePrefix}{msisdn.Trim()}", cancellationToken);
        }
        catch (Exception ex) when (IsCacheUnavailable(ex))
        {
            _logger.LogDebug(ex, "HLR cache read skipped for {Msisdn} (distributed cache unavailable).", msisdn);
            return null;
        }
    }

    private async Task TrySetCachedHlrStateAsync(
        string msisdn,
        string state,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (options is null)
            {
                await _cache.SetStringAsync($"{CachePrefix}{msisdn}", state, cancellationToken);
            }
            else
            {
                await _cache.SetStringAsync($"{CachePrefix}{msisdn}", state, options, cancellationToken);
            }
        }
        catch (Exception ex) when (IsCacheUnavailable(ex))
        {
            _logger.LogDebug(ex, "HLR cache write skipped for {Msisdn} (distributed cache unavailable).", msisdn);
        }
    }

    private static bool IsCacheUnavailable(Exception ex)
    {
        var typeName = ex.GetType().FullName ?? string.Empty;
        return typeName.Contains("RedisConnectionException", StringComparison.Ordinal)
               || typeName.Contains("RedisTimeoutException", StringComparison.Ordinal);
    }

    public Task<HlrResyncResult> ResyncFromHlrAsync(HlrResyncRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HLR re-sync profile {ProfileId} MSISDN {Msisdn}", request.SubscriberProfileId, request.Msisdn);
        return Task.FromResult(new HlrResyncResult(true, "تمت مزامنة حالة الخط من HLR (تجريبي)."));
    }

    public async Task<HlrReprovisionResult> ReprovisionSubscriberAsync(
        HlrReprovisionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "HLR reprovision profile {ProfileId} MSISDN={Msisdn} IMSI={Imsi} ICCID={Iccid}",
            request.SubscriberProfileId,
            request.Msisdn,
            request.Imsi,
            request.Iccid);

        if (!string.IsNullOrWhiteSpace(request.Msisdn))
        {
            await TrySetCachedHlrStateAsync(request.Msisdn.Trim(), "ACTIVE", new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24)
            }, cancellationToken);
        }

        var logEntry =
            $"HlrReprovisionSubscriber MSISDN={request.Msisdn} IMSI={request.Imsi} ICCID={request.Iccid} -> Status: OK";

        return new HlrReprovisionResult(
            true,
            "تمت إعادة تزويد ملف المشترك على HLR بنجاح.",
            logEntry,
            "ACTIVE");
    }

    public async Task MarkMockSubscriberActiveAsync(string msisdn, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(msisdn))
        {
            await TrySetCachedHlrStateAsync(msisdn.Trim(), "ACTIVE", cancellationToken: cancellationToken);
            _logger.LogInformation("HLR mock state forced ACTIVE for {Msisdn}", msisdn);
        }
    }

    public async Task MarkMockSubscriberSuspendedAsync(string msisdn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(msisdn))
        {
            return;
        }

        var normalized = msisdn.Trim();
        await TrySetCachedHlrStateAsync(normalized, "SUSPENDED", cancellationToken: cancellationToken);

        if (_useHttp && _simulator != null)
        {
            try
            {
                await _simulator.SuspendSubscriberAsync(normalized, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HLR simulator suspend skipped for {Msisdn}", normalized);
            }
        }

        _logger.LogInformation("HLR mock state forced SUSPENDED for {Msisdn}", msisdn);
    }

    public async Task MarkMockSubscriberTerminatedAsync(string msisdn, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(msisdn))
        {
            await TrySetCachedHlrStateAsync(msisdn.Trim(), "INACTIVE", cancellationToken: cancellationToken);
            _logger.LogInformation("HLR mock state forced INACTIVE (Killed) for {Msisdn}", msisdn);
        }
    }

    private static string ResolveMockHlrState(int lastDigit) => lastDigit switch
    {
        1 or 4 or 7 => "INACTIVE",
        3 or 9 => "NOT_PROVISIONED",
        5 => "SUSPENDED",
        _ => "ACTIVE",
    };

    private static HlrLiveStatusResult BuildResult(
        string msisdn,
        string hlrState,
        string? crmOperationalStatus,
        string message)
    {
        var hlrActive = string.Equals(hlrState, "ACTIVE", StringComparison.OrdinalIgnoreCase);
        var differs = !StatesAreAligned(crmOperationalStatus, hlrState);

        return new HlrLiveStatusResult(
            true,
            message,
            IsOnline: hlrActive,
            Location: "Damascus-GMSC-01",
            ActiveImsi: "417011234567890",
            HlrSubscriberState: hlrState,
            DiffersFromCrm: differs,
            CrmOperationalStatus: crmOperationalStatus);
    }

    private static bool StatesAreAligned(string? crmOperationalStatus, string hlrState)
    {
        var crm = (crmOperationalStatus ?? string.Empty).Trim();
        var hlr = hlrState.Trim().ToUpperInvariant();

        if (IsCrmActive(crm))
        {
            return hlr == "ACTIVE";
        }

        if (IsCrmSuspended(crm))
        {
            return hlr is "SUSPENDED" or "INACTIVE";
        }

        if (IsCrmTerminated(crm))
        {
            return hlr is "INACTIVE" or "NOT_PROVISIONED";
        }

        return hlr != "ACTIVE";
    }

    private static bool IsCrmActive(string crm) =>
        string.Equals(crm, "Active", StringComparison.OrdinalIgnoreCase) || crm == "1";

    private static bool IsCrmSuspended(string crm) =>
        string.Equals(crm, "Suspended", StringComparison.OrdinalIgnoreCase)
        || string.Equals(crm, "SuspendedInbound", StringComparison.OrdinalIgnoreCase)
        || string.Equals(crm, "SuspendedOutbound", StringComparison.OrdinalIgnoreCase);

    private static bool IsCrmTerminated(string crm) =>
        string.Equals(crm, "Terminated", StringComparison.OrdinalIgnoreCase)
        || string.Equals(crm, "Deactivated", StringComparison.OrdinalIgnoreCase);
}
