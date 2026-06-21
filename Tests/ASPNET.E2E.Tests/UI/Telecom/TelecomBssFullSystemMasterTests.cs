using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Application.Common.Telecom;
using Application.Common.Telecom.Suspension;
using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Domain.Enums;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.UI.Telecom;

/// <summary>
/// Full BSS system E2E — Hub + Customer List + Customer 360 + cross-surface flows.
/// Run: dotnet test --filter "FullyQualifiedName~TelecomBssFullSystemMasterTests"
/// </summary>
[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "UI")]
[Trait("Category", "Comprehensive")]
[Trait("Category", "FullSystem")]
public sealed class TelecomBssFullSystemMasterTests
{
    private const decimal RechargeAmount = 3_000m;
    private const string VasServiceCode = "VAS_CALLER_ID";

    private readonly TelecomE2EFixture _fixture;

    public TelecomBssFullSystemMasterTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task FullSystem_Complete_EndToEnd_AllCapabilities()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        var healthy = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        var notProv = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseNotProvisioned);
        var suspended = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseOperationalSuspended);
        var debt = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services, TelecomDemoMsisdn.DebtSubscriber);
        var fraudLine = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services, TelecomDemoMsisdn.ReconnectFraudDemo);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        var tempFiles = new List<string>();

        try
        {
            (playwright, browser, _, var page) =
                await PlaywrightUiHelper.LaunchComprehensiveMasterPageAsync(_fixture.PublicBaseUrl);

            await PlaywrightUiHelper.LoginViaUiAsync(
                page, _fixture.PublicBaseUrl, PlaywrightUiHelper.ShowroomEmail, PlaywrightUiHelper.DemoPassword);

            // ── Phase A: 14×3 wizard matrix (Hub / List / C360) ──
            await RunHubWizardMatrixAsync(page);
            await RunListWizardMatrixAsync(page, healthy, suspended, tempFiles);
            await RunC360WizardMatrixAsync(page, healthy);

            // ── Phase B: Cross-surface financial & network ──
            await RunHubRechargeAsync(page, healthy.Msisdn);
            await RunListRechargeAsync(page, healthy);
            await RunListHlrQueryAndSyncAsync(page, healthy.Msisdn);
            await RunC360ProfileAndTicketsTabsAsync(page, healthy.CustomerId);

            // ── Phase C: Advanced clearance & regulatory paths ──
            await RunC360FraudSuspensionBackOfficeDraftAsync(page, healthy, tempFiles);
            await RunC360PortInChangeNumberAsync(page, healthy);
            await RunC360MgrScheduledMigrationAsync(page, healthy);
            await RunC360RcnPaymentClearanceAsync(page, debt);
            await RunC360CgtRegulatoryPathAsync(page, healthy, tempFiles);
            await RunC360TrmFraudBackOfficeDraftAsync(page, healthy, tempFiles);
            await RunC360SimLostIdentityGateAsync(page, healthy, tempFiles);
            await RunC360DevInstallmentPathAsync(page, healthy);
            await RunC360BdrPaymentRecordedAsync(page, debt);
            await RunC360TkoDraftAsync(page, healthy, tempFiles);
            await RunListVasToggleMatrixAsync(page, healthy);
            await RunC360TimelineAllFiltersAsync(page, healthy.CustomerId);
            await RunC360HlrReprovisionNotProvisionedAsync(page, notProv);
            await RunListReconnectOnSuspendedLineAsync(page, suspended, tempFiles);
            await RunListFraudSuspensionAsync(page, healthy, tempFiles);
            await RunHubSupportAndActHeaderAsync(page, healthy.CustomerId);

            // ── Phase D: API seed + UI timeline verify ──
            await RunApiOperationAppearsInC360TimelineAsync(page, healthy);

            // ── Phase E: Call center + ticket resolution ──
            await RunCallCenterTicketResolvedTimelineAsync(page, healthy);

            // ── Phase F: Fraud reconnect line (RCN fraud clearance) ──
            await RunC360ReconnectFraudClearanceAsync(page, fraudLine, tempFiles);
        }
        finally
        {
            foreach (var path in tempFiles.Where(File.Exists))
            {
                try { File.Delete(path); } catch { /* best-effort */ }
            }

            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableTheory]
    [MemberData(nameof(HubScenarioMemberData))]
    public async Task FullSystem_HubWizard_MatrixSmoke(string hubTile, string anchorTestId, string personaEmail)
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");
        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            (playwright, browser, _, var page) =
                await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl, personaEmail, PlaywrightUiHelper.DemoPassword);
            await PlaywrightUiHelper.GotoTelecomHubAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightBssWizardHelper.SmokeHubWizardAsync(page, hubTile, anchorTestId);
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    public static IEnumerable<object[]> HubScenarioMemberData() =>
        TelecomBssWizardScenarioCatalog.All.Select(s =>
            new object[] { s.HubTile, s.HubAnchorTestId, HubPersonaEmailForCode(s.Code) });

    // Hub PBAC surface buckets — each wizard renders only for the persona holding its hub permission token.
    private static readonly string[] HubFrontlineCodes = ["ACT", "MGR", "SIM", "CNR"];
    private static readonly string[] HubBackOfficeCodes = ["CGT", "TKO", "TRM", "SUS", "BDR"];
    private static readonly string[] HubSupervisorCodes = ["RCN", "RFD", "DEV", "VAS", "SUP"];

    private static string HubPersonaEmailForCode(string code) =>
        HubBackOfficeCodes.Contains(code) ? PlaywrightUiHelper.BackOfficeEmail
        : HubSupervisorCodes.Contains(code) ? PlaywrightUiHelper.SupervisorEmail
        : PlaywrightUiHelper.ShowroomEmail;

    private async Task RunHubWizardMatrixAsync(IPage page)
    {
        // Hub morphs by PBAC bucket — exercise each wizard with the persona that holds its surface permission.
        await RunHubWizardSubsetAsync(page, PlaywrightUiHelper.ShowroomEmail, HubFrontlineCodes);
        await RunHubWizardSubsetAsync(page, PlaywrightUiHelper.BackOfficeEmail, HubBackOfficeCodes);
        await RunHubWizardSubsetAsync(page, PlaywrightUiHelper.SupervisorEmail, HubSupervisorCodes);

        // Restore the front-line session for the remaining (List / C360) front-desk flows.
        await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page, _fixture.PublicBaseUrl, PlaywrightUiHelper.ShowroomEmail, PlaywrightUiHelper.DemoPassword);
    }

    private async Task RunHubWizardSubsetAsync(IPage page, string email, IReadOnlyList<string> codes)
    {
        await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(page, _fixture.PublicBaseUrl, email, PlaywrightUiHelper.DemoPassword);
        await PlaywrightUiHelper.GotoTelecomHubAsync(page, _fixture.PublicBaseUrl);

        foreach (var scenario in TelecomBssWizardScenarioCatalog.All.Where(s => codes.Contains(s.Code)))
        {
            await PlaywrightBssWizardHelper.SmokeHubWizardAsync(page, scenario.HubTile, scenario.HubAnchorTestId);
        }
    }

    private async Task RunListWizardMatrixAsync(
        IPage page,
        ShowcaseLineRef healthy,
        ShowcaseLineRef suspended,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, healthy.CustomerId);

        foreach (var scenario in TelecomBssWizardScenarioCatalog.All)
        {
            if (scenario.Code == "ACT")
            {
                await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Activate line" }).ClickAsync();
                await Assertions.Expect(page.Locator("[data-testid='list-act-effective-mode']")).ToBeVisibleAsync();
                await PlaywrightBssWizardHelper.CloseListModalAsync(page, scenario.ListModalId);
                continue;
            }

            if (scenario.Code == "SUP")
            {
                await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Support ticket" }).ClickAsync();
                await Assertions.Expect(page.Locator("[data-testid='list-support-issue-type']")).ToBeVisibleAsync();
                await PlaywrightBssWizardHelper.CloseListModalAsync(page, scenario.ListModalId);
                continue;
            }

            if (scenario.ListLineAction is null || scenario.ListAnchorTestId is null)
            {
                continue;
            }

            var msisdn = scenario.Code == "RCN" ? suspended.Msisdn : healthy.Msisdn;
            await PlaywrightBssWizardHelper.SmokeListWizardAsync(
                page,
                msisdn,
                scenario.ListLineAction,
                scenario.ListAnchorTestId,
                scenario.ListModalId);
        }
    }

    private async Task RunC360WizardMatrixAsync(IPage page, ShowcaseLineRef line)
    {
        foreach (var scenario in TelecomBssWizardScenarioCatalog.All)
        {
            await PlaywrightBssWizardHelper.SmokeC360WizardAsync(
                page,
                _fixture.PublicBaseUrl,
                line.CustomerId,
                scenario.C360WizardKind,
                scenario.C360AnchorTestId,
                scenario.Code is "ACT" or "SUP" ? null : line.LineKey);
        }
    }

    private async Task RunHubRechargeAsync(IPage page, string msisdn)
    {
        await PlaywrightUiHelper.GotoTelecomHubAsync(page, _fixture.PublicBaseUrl);
        await page.Locator("#telecomSearchInput").FillAsync(msisdn);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameRegex = new Regex("Recharge|شحن", RegexOptions.IgnoreCase) }).First.ClickAsync();
        var gatewayRef = $"E2E-HUB-{Guid.NewGuid():N}"[..20];
        await PlaywrightUiHelper.CompleteWalletRechargeSwalAsync(page, RechargeAmount, gatewayRef);
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);
    }

    private async Task RunListRechargeAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        var card = page.Locator(".customer-360-asset-card").Filter(new LocatorFilterOptions { HasText = line.Msisdn });
        await card.First.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new Regex("Recharge|شحن", RegexOptions.IgnoreCase) }).ClickAsync();
        var gatewayRef = $"E2E-LST-{Guid.NewGuid():N}"[..20];
        await PlaywrightUiHelper.CompleteWalletRechargeSwalAsync(page, RechargeAmount, gatewayRef);
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        var ledger = await E2ETestDataHelper.GetLatestCompletedRechargeByGatewayRefAsync(
            _fixture.AppFactory.Services, line.CustomerId, gatewayRef);
        Assert.NotNull(ledger);
    }

    private async Task RunListHlrQueryAndSyncAsync(IPage page, string msisdn)
    {
        await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(
            page, _fixture.PublicBaseUrl,
            await E2ETestDataHelper.ResolveDemoCustomerIdAsync(_fixture.AppFactory.Services));

        var card = page.Locator(".customer-360-asset-card").Filter(new LocatorFilterOptions { HasText = msisdn });
        await card.First.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "HLR" }).First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await card.First.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Sync" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task RunC360ProfileAndTicketsTabsAsync(IPage page, string customerId)
    {
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, customerId);
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Profile");
        await Assertions.Expect(page.GetByText(new Regex("billing|CBS|فوترة", RegexOptions.IgnoreCase)).First)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        await PlaywrightUiHelper.ClickC360TabAsync(page, "Tickets");
        await page.Locator(".list-group-item, .table tbody tr, .text-muted").First.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    private async Task RunC360FraudSuspensionBackOfficeDraftAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "suspension", line.LineKey);
        await page.Locator("[data-testid='c360-sus-suspension-type']").SelectOptionAsync(SuspensionWellKnown.Fraud);
        await Assertions.Expect(page.Locator("[data-testid='c360-sus-fraud-clearance']")).ToBeVisibleAsync();

        await page.Locator("#c360ProvWizardModal input[type='text']").First.FillAsync("E2E fraud pattern detected");
        await page.Locator("#c360ProvWizardModal input[dir='ltr']").First.FillAsync("TT-E2E-FRAUD");
        await page.Locator("[data-testid='c360-sus-fraud-clearance'] input[type='checkbox']").First.CheckAsync();

        var kyc = CreateTempPdf(tempFiles);
        await page.Locator("#c360ProvWizardModal input[type='file']").Last.SetInputFilesAsync(kyc);

        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        var submit = page.Locator("#c360ProvWizardModal .modal-footer .btn-danger");
        await submit.ClickAsync();
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            _fixture.AppFactory.Services, line.SubscriberProfileId, TelecomOperationKind.TemporarySuspension);
        Assert.NotNull(op);
        Assert.NotEqual(TelecomOperationStatus.Failed, op!.Status);
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunC360PortInChangeNumberAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "changeNumber", line.LineKey);
        await page.Locator("[data-testid='c360-cn-change-mode']").SelectOptionAsync("PortIn");
        await page.Locator("[data-testid='c360-cn-port-in-msisdn']").FillAsync("09381112233");
        await page.Locator("[data-testid='c360-cn-donor-operator']").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await Assertions.Expect(page.Locator("[data-testid='c360-effective-date-block']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunC360MgrScheduledMigrationAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "migrate", line.LineKey);
        await page.Locator("[data-testid='c360-effective-mode']").SelectOptionAsync("scheduled");
        await Assertions.Expect(page.Locator("[data-testid='c360-effective-date']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunC360RcnPaymentClearanceAsync(IPage page, ShowcaseLineRef debtLine)
    {
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, debtLine.CustomerId);
        await PlaywrightBssWizardHelper.SelectC360LineByMsisdnAsync(page, debtLine.Msisdn);
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, debtLine.CustomerId, "reconnect", debtLine.LineKey);

        await page.Locator("[data-testid='c360-rcn-clearance-type']").SelectOptionAsync("Payment");
        await page.Locator("[data-testid='c360-rcn-payment-ref']").FillAsync($"PAY-RCN-{Guid.NewGuid():N}"[..16]);
        await Assertions.Expect(page.Locator("[data-testid='c360-rcn-payment-ref']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunC360CgtRegulatoryPathAsync(IPage page, ShowcaseLineRef line, ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "changeGsm", line.LineKey);
        await page.Locator("[data-testid='c360-cgt-migration-path']").SelectOptionAsync("Regulatory");
        await page.Locator("[data-testid='c360-cgt-regulatory-doc-no']").FillAsync("REG-2026-E2E");
        await page.Locator("[data-testid='c360-cgt-regulatory-upload']").SetInputFilesAsync(CreateTempPdf(tempFiles));
        await Assertions.Expect(page.Locator("[data-testid='c360-cgt-regulatory-doc-no']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunC360TrmFraudBackOfficeDraftAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "termination", line.LineKey);
        await page.Locator("[data-testid='c360-trm-termination-type']").SelectOptionAsync("Fraud");
        await page.Locator("[data-testid='c360-trm-fraud-ticket']").FillAsync("TT-E2E-TRM-FRAUD");
        await page.Locator("#c360ProvWizardModal input[type='text']").First.FillAsync("E2E fraud termination review");

        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        await PlaywrightBssWizardHelper.CreateC360WizardDraftAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            _fixture.AppFactory.Services, line.SubscriberProfileId, TelecomOperationKind.Termination);
        Assert.NotNull(op);
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunC360SimLostIdentityGateAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "simswap", line.LineKey);
        var reasonSelect = page.Locator("[data-testid='c360-sim-replacement-reason']");
        var options = await reasonSelect.Locator("option").AllInnerTextsAsync();
        var lostIdx = -1;
        for (var i = 0; i < options.Count; i++)
        {
            if (options[i].Contains("Lost", StringComparison.OrdinalIgnoreCase)
                || options[i].Contains("فقد", StringComparison.OrdinalIgnoreCase))
            {
                lostIdx = i;
                break;
            }
        }
        if (lostIdx >= 0)
        {
            await reasonSelect.SelectOptionAsync(new SelectOptionValue { Index = lostIdx });
        }

        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        await PlaywrightBssWizardHelper.CreateC360WizardDraftAsync(page);
        await page.Locator("#c360ProvWizardModal input[type='file']").First.SetInputFilesAsync(CreateTempPdf(tempFiles));
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunC360DevInstallmentPathAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "deviceSale", line.LineKey);
        await page.Locator("[data-testid='c360-dev-sale-type']").SelectOptionAsync("Installment");
        await Assertions.Expect(page.Locator("[data-testid='c360-dev-installment-plan']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunC360BdrPaymentRecordedAsync(IPage page, ShowcaseLineRef debtLine)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, debtLine.CustomerId, "badDebt", debtLine.LineKey);
        await page.Locator("[data-testid='c360-bdr-collection-action']").SelectOptionAsync("PaymentRecorded");
        await page.Locator("#c360ProvWizardModal input[dir='ltr']").First.FillAsync($"PAY-BDR-{Guid.NewGuid():N}"[..14]);
        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        await PlaywrightBssWizardHelper.CreateC360WizardDraftAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            _fixture.AppFactory.Services, debtLine.SubscriberProfileId, TelecomOperationKind.BadDebtRecovery);
        Assert.NotNull(op);
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunC360TkoDraftAsync(IPage page, ShowcaseLineRef line, ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "takeover", line.LineKey);
        await page.Locator("[data-testid='c360-tko-deposit-policy']").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await page.Locator("#c360ProvWizardModal input[type='text']").First.FillAsync("E2E takeover transfer reason");

        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        await PlaywrightBssWizardHelper.CreateC360WizardDraftAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            _fixture.AppFactory.Services, line.SubscriberProfileId, TelecomOperationKind.TakeOver);
        Assert.NotNull(op);
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunListVasToggleMatrixAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await PlaywrightBssWizardHelper.SmokeListWizardAsync(page, line.Msisdn, "VAS", "list-vas-wizard", "#C360VasModal");

        var checkbox = page.Locator(".customer-360-asset-card")
            .Filter(new LocatorFilterOptions { HasText = line.Msisdn })
            .Locator("input[type='checkbox']").First;
        if (await checkbox.CountAsync() > 0)
        {
            await checkbox.CheckAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    private async Task RunC360TimelineAllFiltersAsync(IPage page, string customerId)
    {
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, customerId);
        await PlaywrightBssWizardHelper.RunC360TimelineFiltersAsync(page, TelecomBssWizardScenarioCatalog.TimelineFilterLabels);
    }

    private async Task RunC360HlrReprovisionNotProvisionedAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await PlaywrightBssWizardHelper.SelectC360LineByMsisdnAsync(page, line.Msisdn);

        var reprovision = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = new Regex("HLR reprovision|reprovision|إعادة", RegexOptions.IgnoreCase),
        });
        if (await reprovision.CountAsync() > 0)
        {
            await reprovision.First.ClickAsync();
            await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);
        }
    }

    private async Task RunListReconnectOnSuspendedLineAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await PlaywrightBssWizardHelper.OpenListLineActionAsync(page, line.Msisdn, "Reconnect");
        await page.Locator("[data-testid='list-rcn-clearance-type']").SelectOptionAsync("Operational");
        await page.Locator("#C360ReconnectModal input[type='text']").First.FillAsync("E2E list operational reconnect");
        await PlaywrightBssWizardHelper.CloseListModalAsync(page, "#C360ReconnectModal");
    }

    private async Task RunListFraudSuspensionAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await PlaywrightBssWizardHelper.OpenListLineActionAsync(page, line.Msisdn, "Suspend");
        await page.Locator("[data-testid='list-sus-suspension-type']").SelectOptionAsync(SuspensionWellKnown.Fraud);
        await Assertions.Expect(page.Locator("[data-testid='list-sus-fraud-clearance']")).ToBeVisibleAsync();
        await PlaywrightBssWizardHelper.CloseListModalAsync(page, "#C360SuspensionModal");
    }

    private async Task RunHubSupportAndActHeaderAsync(IPage page, string customerId)
    {
        // Support (SUP) lives on the supervisor surface; activation (ACT) on the front-line surface.
        await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page, _fixture.PublicBaseUrl, PlaywrightUiHelper.SupervisorEmail, PlaywrightUiHelper.DemoPassword);
        await PlaywrightUiHelper.GotoTelecomHubAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightBssWizardHelper.SmokeHubWizardAsync(page, "support", "hub-support-issue-type");

        await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page, _fixture.PublicBaseUrl, PlaywrightUiHelper.ShowroomEmail, PlaywrightUiHelper.DemoPassword);
        await PlaywrightUiHelper.GotoTelecomHubAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightBssWizardHelper.SmokeHubWizardAsync(page, "activate", "hub-act-line-type");
    }

    private async Task RunApiOperationAppearsInC360TimelineAsync(IPage page, ShowcaseLineRef line)
    {
        var client = CreateApiClient();
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.ServiceModification,
            "fullsystem-vas-timeline",
            CancellationToken.None);

        var operationId = await client.CreateOperationAsync(new
        {
            kind = (int)TelecomOperationKind.ServiceModification,
            subscriberProfileId = seed.Line.SubscriberProfileId,
            msisdnAssetId = seed.Line.MsisdnAssetId,
            notes = "Activate VAS VAS_CALLER_ID",
        });
        await client.UploadDocumentAsync(operationId);
        await client.ConfirmAsync(operationId);

        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Timeline");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Operations" }).ClickAsync();
        await page.Locator(".list-group-item").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
    }

    private async Task RunCallCenterTicketResolvedTimelineAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page, _fixture.PublicBaseUrl, PlaywrightUiHelper.CallCenterEmail, PlaywrightUiHelper.DemoPassword);
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameRegex = new Regex("Support ticket|تذكرة", RegexOptions.IgnoreCase) }).ClickAsync();
        await page.Locator("[data-testid='c360-support-issue-type']").SelectOptionAsync("1");
        await page.Locator("[data-testid='c360-support-wizard'] textarea").FillAsync("Billing dispute — incorrect charge on last recharge.");
        await page.Locator("#c360ProvWizardModal").GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new Regex("Create|إنشاء", RegexOptions.IgnoreCase) }).ClickAsync();
        var ticketNumber = (await page.Locator("#c360ProvWizardModal strong").First.InnerTextAsync()).Trim();

        var ticketId = await E2ETestDataHelper.GetTechnicalTicketIdByNumberAsync(_fixture.AppFactory.Services, ticketNumber);
        await E2ETestDataHelper.TransitionTechnicalTicketStatusAsync(
            _fixture.AppFactory.Services, ticketId!, TechnicalTicketStatus.Resolved);

        await PlaywrightUiHelper.ClickC360TabAsync(page, "Timeline");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Tickets" }).ClickAsync();
        var entry = page.Locator(".list-group-item").Filter(new LocatorFilterOptions { HasText = ticketNumber });
        await entry.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var text = await entry.First.InnerTextAsync();
        Assert.Contains("Resolved", text, StringComparison.OrdinalIgnoreCase);
    }

    private async Task RunC360ReconnectFraudClearanceAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page, _fixture.PublicBaseUrl, PlaywrightUiHelper.ShowroomEmail, PlaywrightUiHelper.DemoPassword);
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await PlaywrightBssWizardHelper.SelectC360LineByMsisdnAsync(page, line.Msisdn);

        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "reconnect", line.LineKey);
        await page.Locator("[data-testid='c360-rcn-clearance-type']").SelectOptionAsync("Fraud");
        await page.Locator("#c360ProvWizardModal input[dir='ltr']").First.FillAsync("TT-FRAUD-RCN-E2E");
        await Assertions.Expect(page.Locator("#c360ProvWizardModal .alert")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private TelecomE2EClient CreateApiClient()
    {
        var http = _fixture.AppFactory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        return new TelecomE2EClient(http, _fixture.SimulatorClient);
    }

    private static string CreateTempPdf(ICollection<string> registry)
    {
        var path = Path.Combine(Path.GetTempPath(), $"bss-fullsystem-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, "%PDF-1.4 BSS Full System E2E Placeholder", Encoding.UTF8);
        registry.Add(path);
        return path;
    }
}
