using System.Collections.Concurrent;
using Application.Common.Integrations;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

public sealed class HlrLiveStatusService : IHLRLiveStatusService
{
    private static readonly ConcurrentDictionary<string, string> ReprovisionedStates = new(StringComparer.Ordinal);

    private readonly ILogger<HlrLiveStatusService> _logger;

    public HlrLiveStatusService(ILogger<HlrLiveStatusService> logger) => _logger = logger;

    public Task<HlrLiveStatusResult> QueryLiveStatusAsync(
        string msisdn,
        string? crmOperationalStatus,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HLR live query for {Msisdn}", msisdn);

        if (ReprovisionedStates.TryGetValue(msisdn, out var forcedState))
        {
            return Task.FromResult(BuildResult(msisdn, forcedState, crmOperationalStatus, "HLR live query OK (reprovisioned)"));
        }

        var lastDigit = msisdn.Length > 0 ? msisdn[^1] - '0' : 0;
        var hlrState = ResolveMockHlrState(lastDigit);
        return Task.FromResult(BuildResult(msisdn, hlrState, crmOperationalStatus, "HLR live query OK"));
    }

    public Task<HlrResyncResult> ResyncFromHlrAsync(HlrResyncRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HLR re-sync profile {ProfileId} MSISDN {Msisdn}", request.SubscriberProfileId, request.Msisdn);
        return Task.FromResult(new HlrResyncResult(true, "تمت مزامنة حالة الخط من HLR (تجريبي)."));
    }

    public Task<HlrReprovisionResult> ReprovisionSubscriberAsync(
        HlrReprovisionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "HLR reprovision profile {ProfileId} MSISDN={Msisdn} IMSI={Imsi} ICCID={Iccid}",
            request.SubscriberProfileId,
            request.Msisdn,
            request.Imsi,
            request.Iccid);

        ReprovisionedStates[request.Msisdn] = "ACTIVE";

        var logEntry =
            $"HlrReprovisionSubscriber MSISDN={request.Msisdn} IMSI={request.Imsi} ICCID={request.Iccid} -> Status: OK";

        return Task.FromResult(new HlrReprovisionResult(
            true,
            "تمت إعادة تزويد ملف المشترك على HLR بنجاح.",
            logEntry,
            "ACTIVE"));
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
        var crmActive = string.Equals(crmOperationalStatus, "Active", StringComparison.OrdinalIgnoreCase)
            || crmOperationalStatus == "1";
        var hlrActive = hlrState == "ACTIVE";
        var differs = crmActive != hlrActive;

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
}
