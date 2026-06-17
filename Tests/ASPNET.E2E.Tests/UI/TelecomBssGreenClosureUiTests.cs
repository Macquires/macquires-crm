using ASPNET.E2E.Tests.Infrastructure;
using Application.Common.Telecom;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.UI;

/// <summary>Wave 5+ — Playwright smoke for SUS/RCN/BDR, VAS, MGR/TRM/SIM List wizards.</summary>
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

            await PlaywrightUiHelper.OpenHubWizardAsync(page, "suspension");
            await page.Locator("#hubSusSuspensionType").SelectOptionAsync("Fraud");
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

            await PlaywrightUiHelper.OpenHubWizardAsync(page, "reconnect");
            await page.Locator("#hubRcnClearanceType").SelectOptionAsync("Payment");
            await Assertions.Expect(page.Locator("[data-testid='telecom-rcn-payment-ref']")).ToBeVisibleAsync();
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

            await PlaywrightUiHelper.OpenHubWizardAsync(page, "badDebt");
            await page.Locator("#bdrCollectionAction").SelectOptionAsync("WriteOffPartial");
            await Assertions.Expect(page.Locator("[data-testid='telecom-bdr-writeoff-amount']")).ToBeVisibleAsync();
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

            await PlaywrightUiHelper.OpenHubWizardAsync(page, "addpackage");
            await Assertions.Expect(page.Locator("[data-testid='hub-vas-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='hub-vas-service-select']")).ToBeVisibleAsync();
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

    [SkippableFact]
    public async Task List_SuspensionModal_FraudType_ShowsClearanceFields()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(
                page,
            TelecomDemoMsisdn.ShowcaseHealthy,
                "Suspend");

            await page.Locator("#C360SuspensionModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await page.Locator("#listSusSuspensionType").SelectOptionAsync("Fraud");
            await Assertions.Expect(page.Locator("[data-testid='list-sus-fraud-clearance']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_ReconnectModal_PaymentClearance_ShowsPaymentReference()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            Application.Common.Telecom.TelecomDemoMsisdn.ShowcaseOperationalSuspended);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(
                page,
                TelecomDemoMsisdn.ShowcaseOperationalSuspended,
                "Reconnect");

            await page.Locator("#C360ReconnectModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await page.Locator("#listRcnClearanceType").SelectOptionAsync("Payment");
            await Assertions.Expect(page.Locator("[data-testid='list-rcn-payment-ref']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='list-rcn-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_BadDebtModal_WriteOffPartial_ShowsAmountField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            Application.Common.Telecom.TelecomDemoMsisdn.DebtSubscriber);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(
                page,
                TelecomDemoMsisdn.DebtSubscriber,
                "Collection");

            await page.Locator("#C360BadDebtModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await page.Locator("#listBdrCollectionAction").SelectOptionAsync("WriteOffPartial");
            await Assertions.Expect(page.Locator("[data-testid='list-bdr-writeoff-amount']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='list-bdr-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_SuspensionWizard_FraudType_ShowsClearanceFields()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await page.GotoAsync(
                $"{_fixture.PublicBaseUrl}/Telecom/Customer360Profile?customerId={Uri.EscapeDataString(line.CustomerId)}&wizard=suspension&lineKey={Uri.EscapeDataString(line.LineKey)}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator("#c360ProvWizardModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
            await page.Locator("#c360SusSuspensionType").SelectOptionAsync("Fraud");
            await Assertions.Expect(page.Locator("[data-testid='c360-sus-fraud-clearance']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_MigrateModal_HasOfferingSelect()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(
                page,
                TelecomDemoMsisdn.ShowcaseHealthy,
                "Migrate");

            await page.Locator("#C360MigrateModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-mgr-offering-select']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_TerminationModal_FraudType_ShowsSecurityTicket()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(
                page,
                TelecomDemoMsisdn.ShowcaseHealthy,
                "Terminate");

            await page.Locator("#C360TerminationModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await page.Locator("#listTrmTerminationType").SelectOptionAsync("Fraud");
            await Assertions.Expect(page.Locator("[data-testid='list-trm-fraud-ticket']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_SimSwapModal_HasReplacementReasonField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(
                page,
                TelecomDemoMsisdn.ShowcaseHealthy,
                "SIM swap");

            await page.Locator("#C360SimSwapModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-sim-replacement-reason']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_VasWizardModal_HasCatalogFields()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(
                page,
                TelecomDemoMsisdn.ShowcaseHealthy,
                "VAS");

            await page.Locator("#C360VasModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-vas-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='list-vas-service-select']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='list-vas-action-select']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_ActivateWizard_HasLineTypeField()
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
            await PlaywrightUiHelper.OpenHubWizardAsync(page, "activate");
            await Assertions.Expect(page.Locator("[data-testid='hub-act-line-type']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='hub-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_MigrateWizard_HasOfferingSelect()
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
            await PlaywrightUiHelper.OpenHubWizardAsync(page, "migrate");
            await Assertions.Expect(page.Locator("[data-testid='hub-mgr-offering-select']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_TerminationWizard_FraudType_ShowsSecurityTicket()
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
            await PlaywrightUiHelper.OpenHubWizardAsync(page, "termination");
            await page.Locator("#hubTrmTerminationType").SelectOptionAsync("Fraud");
            await Assertions.Expect(page.Locator("[data-testid='hub-trm-fraud-ticket']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_SimSwapWizard_HasReplacementReasonField()
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
            await PlaywrightUiHelper.OpenHubWizardAsync(page, "simswap");
            await Assertions.Expect(page.Locator("[data-testid='hub-sim-replacement-reason']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_TakeoverWizard_HasDepositPolicyField()
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
            await PlaywrightUiHelper.OpenHubWizardAsync(page, "takeover");
            await Assertions.Expect(page.Locator("[data-testid='hub-tko-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='hub-tko-deposit-policy']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_SupportWizard_HasIssueTypeField()
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
            await PlaywrightUiHelper.OpenHubWizardAsync(page, "support");
            await Assertions.Expect(page.Locator("[data-testid='hub-support-issue-type']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_RefundWizard_HasEffectiveDateField()
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
            await PlaywrightUiHelper.OpenHubWizardAsync(page, "refund");
            await Assertions.Expect(page.Locator("[data-testid='hub-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_MigrateWizard_HasOfferingSelect()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(_fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "migrate", line.LineKey);
            await Assertions.Expect(page.Locator("[data-testid='c360-mgr-offering-select']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_ReconnectWizard_PaymentClearance_ShowsPaymentReference()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseOperationalSuspended);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "reconnect", line.LineKey);
            await page.Locator("#c360RcnClearanceType").SelectOptionAsync("Payment");
            await Assertions.Expect(page.Locator("[data-testid='c360-rcn-payment-ref']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='c360-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_BadDebtWizard_WriteOffPartial_ShowsAmountField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.DebtSubscriber);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "badDebt", line.LineKey);
            await page.Locator("[data-testid='c360-bdr-collection-action']").SelectOptionAsync("WriteOffPartial");
            await Assertions.Expect(page.Locator("[data-testid='c360-bdr-writeoff-amount']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_TerminationWizard_FraudType_ShowsSecurityTicket()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(_fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "termination", line.LineKey);
            await page.Locator("#c360TrmTerminationType").SelectOptionAsync("Fraud");
            await Assertions.Expect(page.Locator("[data-testid='c360-trm-fraud-ticket']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_SimSwapWizard_HasReplacementReasonField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(_fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "simswap", line.LineKey);
            await Assertions.Expect(page.Locator("[data-testid='c360-sim-replacement-reason']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_VasWizard_HasCatalogFields()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(_fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "addpackage", line.LineKey);
            await Assertions.Expect(page.Locator("[data-testid='c360-vas-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='c360-vas-service-select']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_ActivateWizard_HasLineTypeField()
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
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, customerId, "activate");
            await Assertions.Expect(page.Locator("[data-testid='c360-act-line-type']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='c360-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_TakeoverWizard_HasDepositPolicyField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(_fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "takeover", line.LineKey);
            await Assertions.Expect(page.Locator("[data-testid='c360-tko-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='c360-tko-deposit-policy']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_ChangeNumberWizard_PortInMode_ShowsMnpFields()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(_fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "changeNumber", line.LineKey);
            await page.Locator("[data-testid='c360-cn-change-mode']").SelectOptionAsync("PortIn");
            await Assertions.Expect(page.Locator("[data-testid='c360-cn-port-in-msisdn']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='c360-cn-donor-operator']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_RefundWizard_HasEffectiveDateField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(_fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "refund", line.LineKey);
            await Assertions.Expect(page.Locator("[data-testid='c360-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_DeviceSaleWizard_HasInventorySelect()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(_fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "deviceSale", line.LineKey);
            await Assertions.Expect(page.Locator("[data-testid='c360-dev-inventory-select']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='c360-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_RefundModal_HasEffectiveDateFields()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(page, TelecomDemoMsisdn.ShowcaseHealthy, "Refund");
            await page.Locator("#C360RefundModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-rfd-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='list-rfd-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_DeviceSaleModal_HasEffectiveDateFields()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(page, TelecomDemoMsisdn.ShowcaseHealthy, "Device sale");
            await page.Locator("#C360DeviceSaleModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-dev-effective-mode']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_TakeoverModal_HasDepositPolicyField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(page, TelecomDemoMsisdn.ShowcaseHealthy, "Transfer");
            await page.Locator("#C360TakeOverModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-tko-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='list-tko-deposit-policy']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_ChangeNumberModal_PortInMode_ShowsMnpFields()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(page, TelecomDemoMsisdn.ShowcaseHealthy, "Change number");
            await page.Locator("#C360ChangeNumberModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await page.Locator("[data-testid='list-cn-change-mode']").SelectOptionAsync("PortIn");
            await Assertions.Expect(page.Locator("[data-testid='list-cn-port-in-msisdn']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='list-cn-donor-operator']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_ChangeGsmWizard_HasTargetAndPathFields()
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
            await PlaywrightUiHelper.OpenHubWizardAsync(page, "changeGsm");
            await Assertions.Expect(page.Locator("[data-testid='hub-cgt-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='hub-cgt-target-type']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='hub-cgt-migration-path']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task Hub_DeviceSaleWizard_HasInventorySelect()
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
            await PlaywrightUiHelper.OpenHubWizardAsync(page, "deviceSale");
            await Assertions.Expect(page.Locator("[data-testid='hub-dev-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='hub-dev-inventory-select']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task C360_ChangeGsmWizard_HasTargetTypeField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(_fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "changeGsm", line.LineKey);
            await Assertions.Expect(page.Locator("[data-testid='c360-cgt-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='c360-cgt-target-type']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_ChangeGsmModal_HasTargetTypeField()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(page, TelecomDemoMsisdn.ShowcaseHealthy, "Change line type");
            await page.Locator("#C360ChangeGsmModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-cgt-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='list-cgt-target-type']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task List_DeviceSaleModal_HasInventorySelect()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();
        var customerId = await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) = await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, customerId);
            await PlaywrightUiHelper.OpenListLineActionForMsisdnAsync(page, TelecomDemoMsisdn.ShowcaseHealthy, "Device sale");
            await page.Locator("#C360DeviceSaleModal").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await Assertions.Expect(page.Locator("[data-testid='list-dev-wizard']")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("[data-testid='list-dev-inventory-select']")).ToBeVisibleAsync();
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }
}
