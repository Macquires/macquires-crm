using System.Net;
using ASPNET.E2E.Tests.Infrastructure;

namespace ASPNET.E2E.Tests.UI.LiveDemo;

internal static class LiveDemoInfrastructureGuard
{
    public static async Task VerifyOrThrowAsync(TelecomE2EFixture fixture, CancellationToken cancellationToken = default)
    {
        if (!DockerProbe.IsAvailable())
        {
            throw new LiveDemoInfrastructureException();
        }

        if (fixture.DockerUnavailable)
        {
            throw new LiveDemoInfrastructureException();
        }

        if (string.IsNullOrWhiteSpace(fixture.PublicBaseUrl))
        {
            throw new LiveDemoInfrastructureException(
                "❌ CRITICAL FAILURE: Docker Desktop or the containerized Database is offline! Please ensure Docker Desktop is running before starting the demo.");
        }

        using var client = fixture.AppFactory.CreateClient(new()
        {
            AllowAutoRedirect = true,
            HandleCookies = false,
        });
        client.Timeout = TimeSpan.FromMinutes(2);

        using var response = await client.GetAsync("/health", cancellationToken);
        if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable))
        {
            throw new LiveDemoInfrastructureException(
                $"❌ CRITICAL FAILURE: Application health endpoint returned {response.StatusCode}. Infrastructure is not ready for live execution.");
        }

        LiveDemoConsole.LogSecurityBanner();
    }
}
