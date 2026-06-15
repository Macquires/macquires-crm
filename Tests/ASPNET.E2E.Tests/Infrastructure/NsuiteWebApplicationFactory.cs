using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
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

        builder.ConfigureWebHost(webBuilder => webBuilder.UseKestrel());
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var addresses = _kestrelHost.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses;

        PublicBaseUrl = NormalizeAddress(addresses.First());

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
            });
        });
    }

    private static string NormalizeAddress(string address)
    {
        return address
            .Replace("[::]", "127.0.0.1", StringComparison.Ordinal)
            .Replace("http://[::ffff:", "http://127.0.0.1:", StringComparison.Ordinal)
            .Replace("https://[::ffff:", "https://127.0.0.1:", StringComparison.Ordinal)
            .TrimEnd('/');
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
