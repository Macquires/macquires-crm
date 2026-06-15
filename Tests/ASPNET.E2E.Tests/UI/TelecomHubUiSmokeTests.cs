using ASPNET.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.UI;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "UI")]
public sealed class TelecomHubUiSmokeTests
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomHubUiSmokeTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task TelecomHub_Loads_AndShowsSearchAfterLogin()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoTelecomHubAsync(page, _fixture.PublicBaseUrl);

            await Assertions.Expect(page.Locator("#telecomSearchInput")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#formcard")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }
}
