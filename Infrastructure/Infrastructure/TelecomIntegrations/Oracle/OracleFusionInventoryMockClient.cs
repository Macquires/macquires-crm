using Application.Common.Integrations;

using Microsoft.Extensions.Logging;



namespace Infrastructure.TelecomIntegrations.Oracle;



/// <summary>

/// Demo Oracle Fusion SCM pull — data contract is <see cref="OracleFusionInventoryCatalog"/>.

/// </summary>

public sealed class OracleFusionInventoryMockClient : IOracleFusionInventoryClient

{

    private readonly ILogger<OracleFusionInventoryMockClient> _logger;



    public OracleFusionInventoryMockClient(ILogger<OracleFusionInventoryMockClient> logger) => _logger = logger;



    public Task<OracleInventoryPullResult> PullNewInventoryAsync(

        DateTime? sinceUtc,

        CancellationToken cancellationToken = default)

    {

        _logger.LogInformation("Oracle Fusion SCM mock pull since {Since}", sinceUtc?.ToString("O") ?? "beginning");

        return Task.FromResult(OracleFusionInventoryCatalog.BuildStandardPull());

    }

}


