using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ASPNET.E2E.Tests.Infrastructure;

public sealed class NsuiteWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _sqlConnectionString;
    private readonly string _simulatorBaseUrl;
    private readonly string? _redisConnectionString;
    private IHost? _kestrelHost;

    public string PublicBaseUrl { get; private set; } = string.Empty;

    public NsuiteWebApplicationFactory(string sqlConnectionString, string simulatorBaseUrl, string? redisConnectionString = null)
    {
        _sqlConnectionString = sqlConnectionString;
        _simulatorBaseUrl = simulatorBaseUrl.TrimEnd('/');
        _redisConnectionString = redisConnectionString;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = base.CreateHost(builder);

        var port = AllocateTcpPort();
        var publicUrl = $"http://127.0.0.1:{port}";

        builder.ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseKestrel();
            webBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, publicUrl);
            webBuilder.PreferHostingUrls(true);
        });

        _kestrelHost = builder.Build();
        _kestrelHost.Start();
        PublicBaseUrl = publicUrl;

        return testHost;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlConnectionString,
                ["ConnectionStrings:Redis"] = _redisConnectionString ?? "",
                ["Redis:ConnectionString"] = _redisConnectionString ?? "",
                ["Database:UseEfMigrations"] = "true",
                ["Database:ApplyLegacyPatchesAfterMigrations"] = "false",
                ["IsDemoVersion"] = "true",
                ["TelecomIntegrations:Mode"] = "Http",
                ["TelecomIntegrations:SimulatorBaseUrl"] = _simulatorBaseUrl,
                ["TelecomIntegrations:TimeoutSeconds"] = "20",
                ["TelecomIntegrations:MaxRetryAttempts"] = "2",
                ["TelecomBilling:EnableChaosEngineering"] = "false",
                ["TelecomBilling:FailAttemptsBeforeSuccess"] = "0",
                ["RabbitMq:Enabled"] = "false",
                ["Database:UseOutbox"] = "true",
                ["IntegrationOutbox:DispatchIntervalSeconds"] = "20",
                ["Jwt:Key"] = "E2ETestJwtKeyMustBeAtLeast32CharsLong!",
                ["Jwt:Issuer"] = "nSuite-E2E",
                ["Jwt:Audience"] = "nSuite-E2E",
                ["FieldEncryption:KeyBase64"] = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=",
                ["FieldEncryption:SearchHashKeyBase64"] = "ISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0A=",
                ["AspNetIdentity:DefaultAdmin:Email"] = "admin@root.com",
                ["AspNetIdentity:DefaultAdmin:Password"] = "123456",
                ["KycDocumentStorage:VaultRootPath"] = "wwwroot/secure_kyc_vault_e2e",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
            });
        });
    }

    private static int AllocateTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _kestrelHost is not null)
        {
            _kestrelHost.StopAsync().GetAwaiter().GetResult();
            _kestrelHost.Dispose();
            _kestrelHost = null;
        }

        base.Dispose(disposing);
    }
}
