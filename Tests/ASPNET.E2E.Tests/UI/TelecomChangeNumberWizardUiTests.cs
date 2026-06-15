using ASPNET.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.UI;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "UI")]
public sealed class TelecomChangeNumberWizardUiTests
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomChangeNumberWizardUiTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task ChangeNumberWizard_PortInMode_ShowsMnpFields()
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
            await PlaywrightUiHelper.OpenChangeNumberWizardAsync(page);

            await page.Locator("[data-testid='telecom-cn-change-mode']").SelectOptionAsync("PortIn");

            await Assertions.Expect(page.Locator("[data-testid='telecom-cn-port-in-msisdn']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='telecom-cn-donor-operator']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#telecomOpsWizardPanel")).ToContainTextAsync("MNP", new LocatorAssertionsToContainTextOptions { IgnoreCase = true });
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task ChangeNumberWizard_InternalMode_ShowsPoolSelector()
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
            await PlaywrightUiHelper.OpenChangeNumberWizardAsync(page);

            await page.Locator("[data-testid='telecom-cn-change-mode']").SelectOptionAsync("Internal");

            await Assertions.Expect(page.Locator("[data-testid='telecom-cn-port-in-msisdn']")).ToHaveCountAsync(0);
            await Assertions.Expect(page.Locator("select").Filter(new LocatorFilterOptions
            {
                Has = page.Locator("option[value]:not([value=''])"),
            }).First).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }
}
