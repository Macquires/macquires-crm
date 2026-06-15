using ASPNET.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.UI;

/// <summary>Wave 5 — Playwright smoke for SUS fraud, RCN/BDR, BDR write-off BO, VAS activate/deactivate.</summary>
[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "UI")]
public sealed class TelecomBssGreenClosureUiTests
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomBssGreenClosureUiTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task Hub_Loads_SusFraudWizardFields()
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

            await page.Locator("#telecomOpsWizardKind").SelectOptionAsync("suspension");
            await Assertions.Expect(page.Locator("#telecomOpsWizardPanel")).ToBeVisibleAsync();
            await page.Locator("#susSuspensionType").SelectOptionAsync("Fraud");
            await Assertions.Expect(page.Locator("[data-testid='telecom-sus-fraud-clearance']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_Loads_RcnPaymentReferenceField()
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

            await page.Locator("#telecomOpsWizardKind").SelectOptionAsync("reconnect");
            await Assertions.Expect(page.Locator("#telecomOpsWizardPanel")).ToBeVisibleAsync();
            await page.Locator("#rcnClearanceType").SelectOptionAsync("Payment");
            await Assertions.Expect(page.Locator("#rcnPaymentReference, [data-testid='telecom-rcn-payment-ref']").First).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_Loads_BdrWriteOffAmountField()
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

            await page.Locator("#telecomOpsWizardKind").SelectOptionAsync("badDebt");
            await Assertions.Expect(page.Locator("#telecomOpsWizardPanel")).ToBeVisibleAsync();
            await page.Locator("#bdrCollectionAction").SelectOptionAsync("WriteOffPartial");
            await Assertions.Expect(page.Locator("#bdrWriteOffAmount, [data-testid='telecom-bdr-writeoff-amount']").First).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_Loads_VasWizardPanel()
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

            await page.Locator("#telecomOpsWizardKind").SelectOptionAsync("addpackage");
            await Assertions.Expect(page.Locator("#telecomOpsWizardPanel")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#telecomOpsWizardPanel")).ToContainTextAsync("VAS", new LocatorAssertionsToContainTextOptions { IgnoreCase = true });
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_NewLineModal_HasEffectiveDateFields()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdAsync(_fixture.AppFactory.Services);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);

            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Activate line" }).ClickAsync();
            await page.Locator("#C360NewLineModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-act-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_SupportModal_HasIssueTypeField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdAsync(_fixture.AppFactory.Services);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);

            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Support ticket" }).ClickAsync();
            await page.Locator("#C360SupportTicketModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-support-issue-type']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_SupportWizard_HasIssueTypeField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var seed = await E2ETestDataHelper.ResolveActivationSeedAsync(_fixture.AppFactory.Services);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await page.GotoAsync(
                $"{_fixture.PublicBaseUrl}/Telecom/Customer360Profile?customerId={Uri.EscapeDataString(seed.CustomerId)}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Support ticket" }).ClickAsync();
            await page.Locator("[data-testid='c360-support-wizard']").WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='c360-support-issue-type']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }
}
