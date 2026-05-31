using Application.Common.Integrations;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>Placeholder directory/HLR sync — replace with real Huawei (or other) API when available.</summary>
public sealed class TelecomDirectoryMockSyncIntegration : ITelecomDirectorySync
{
    private readonly ILogger<TelecomDirectoryMockSyncIntegration> _logger;

    public TelecomDirectoryMockSyncIntegration(ILogger<TelecomDirectoryMockSyncIntegration> logger)
    {
        _logger = logger;
    }

    public Task<TelecomDirectorySyncResult> NotifyMsisdnChangedAsync(
        TelecomDirectoryMsisdnChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Telecom directory mock: MSISDN change customer={CustomerId} asset={AssetId} {Old} -> {New}",
            request.CustomerId,
            request.MsisdnAssetId,
            request.OldMsisdn,
            request.NewMsisdn);

        return Task.FromResult(new TelecomDirectorySyncResult(true, "HuaweiDirectoryMock: OK"));
    }
}
