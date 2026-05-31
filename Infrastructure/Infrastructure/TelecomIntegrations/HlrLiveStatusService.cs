using Application.Common.Integrations;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

public sealed class HlrLiveStatusService : IHLRLiveStatusService
{
    private readonly ILogger<HlrLiveStatusService> _logger;

    public HlrLiveStatusService(ILogger<HlrLiveStatusService> logger) => _logger = logger;

    public Task<HlrLiveStatusResult> QueryLiveStatusAsync(
        string msisdn,
        string? crmOperationalStatus,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HLR live query for {Msisdn}", msisdn);

        var lastDigit = msisdn.Length > 0 ? msisdn[^1] - '0' : 0;
        var hlrState = lastDigit % 2 == 0 ? "ACTIVE" : "SUSPENDED";
        var crmActive = string.Equals(crmOperationalStatus, "Active", StringComparison.OrdinalIgnoreCase)
            || crmOperationalStatus == "1";
        var hlrActive = hlrState == "ACTIVE";
        var differs = crmActive != hlrActive;

        return Task.FromResult(new HlrLiveStatusResult(
            true,
            "HLR live query OK",
            IsOnline: hlrActive,
            Location: "Damascus-GMSC-01",
            ActiveImsi: "417011234567890",
            HlrSubscriberState: hlrState,
            DiffersFromCrm: differs,
            CrmOperationalStatus: crmOperationalStatus));
    }

    public Task<HlrResyncResult> ResyncFromHlrAsync(HlrResyncRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HLR re-sync profile {ProfileId} MSISDN {Msisdn}", request.SubscriberProfileId, request.Msisdn);
        return Task.FromResult(new HlrResyncResult(true, "تمت مزامنة حالة الخط من HLR (تجريبي)."));
    }
}
