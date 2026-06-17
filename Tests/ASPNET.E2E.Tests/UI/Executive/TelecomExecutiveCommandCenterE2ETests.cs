using System.Text.RegularExpressions;
using ASPNET.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.UI.Executive;

/// <summary>
/// E2E for Executive Command Center (/Executive/CommandCenter) — GM MIS dashboard.
/// Run: dotnet test --filter "FullyQualifiedName~TelecomExecutiveCommandCenterE2ETests"
/// </summary>
[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "UI")]
[Trait("Category", "Executive")]
[Trait("Category", "Comprehensive")]
public sealed class TelecomExecutiveCommandCenterE2ETests
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomExecutiveCommandCenterE2ETests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task ExecutiveCommandCenter_GmMasterSuite_AllTabsScopeAndPanels()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            (playwright, browser, _, var page) =
                await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl, headless: true);

            await PlaywrightUiHelper.LoginViaUiAsync(
                page,
                _fixture.PublicBaseUrl,
                PlaywrightUiHelper.ExecutiveMisEmail,
                PlaywrightUiHelper.DemoPassword);

            await PlaywrightUiHelper.InstallExecutiveAlertsSnifferAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightExecutiveCommandCenterHelper.GotoCommandCenterAsync(page, _fixture.PublicBaseUrl);

            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions
            {
                NameRegex = new Regex("Executive Command Center|مركز قيادة", RegexOptions.IgnoreCase),
            })).ToBeVisibleAsync();

            await PlaywrightExecutiveCommandCenterHelper.AssertGmScopeFiltersVisibleAsync(page);

            // Scorecard (default tab)
            await PlaywrightExecutiveCommandCenterHelper.WaitForScorecardKpisAsync(page);

            // Scope apply reloads all panels
            var fromInput = page.Locator(".executive-scope-bar input[type='date']").First;
            var fromValue = await fromInput.InputValueAsync();
            if (!string.IsNullOrWhiteSpace(fromValue) && DateTime.TryParse(fromValue, out var fromDate))
            {
                await fromInput.FillAsync(fromDate.AddDays(-7).ToString("yyyy-MM-dd"));
                await PlaywrightExecutiveCommandCenterHelper.ApplyScopeAsync(page);
                await PlaywrightExecutiveCommandCenterHelper.WaitForScorecardKpisAsync(page);
            }

            foreach (var (tabId, labelPattern) in PlaywrightExecutiveCommandCenterHelper.Tabs)
            {
                await PlaywrightExecutiveCommandCenterHelper.ClickTabAsync(page, tabId, labelPattern);
            }

            await PlaywrightExecutiveCommandCenterHelper.ClickTabAsync(page, "cc-network", "Network");
            await PlaywrightExecutiveCommandCenterHelper.WaitForNetworkPanelAsync(page);

            await PlaywrightExecutiveCommandCenterHelper.ClickTabAsync(page, "cc-mis", "MIS");
            await PlaywrightExecutiveCommandCenterHelper.WaitForMisPanelAsync(page);

            await PlaywrightExecutiveCommandCenterHelper.ClickTabAsync(page, "cc-operations", "Operations");
            await PlaywrightExecutiveCommandCenterHelper.WaitForOperationsPanelAsync(page);

            await PlaywrightExecutiveCommandCenterHelper.ClickTabAsync(page, "cc-workforce", "Workforce");
            await PlaywrightExecutiveCommandCenterHelper.WaitForWorkforcePanelAsync(page);
            await PlaywrightExecutiveCommandCenterHelper.OpenFirstWorkforceDetailAsync(page);

            await PlaywrightExecutiveCommandCenterHelper.ClickTabAsync(page, "cc-alerts", "Alerts");
            await PlaywrightExecutiveCommandCenterHelper.WaitForAlertsPanelAsync(page);
            await PlaywrightExecutiveCommandCenterHelper.SwitchAlertsSupervisorSubTabAsync(page);

            _ = await PlaywrightUiHelper.ReadExecutiveAlertCountAsync(page);
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task ExecutiveCommandCenter_ShowroomUser_SeesPermissionGate()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            (playwright, browser, _, var page) =
                await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl, headless: true);

            await PlaywrightUiHelper.LoginViaUiAsync(
                page,
                _fixture.PublicBaseUrl,
                PlaywrightUiHelper.ShowroomEmail,
                PlaywrightUiHelper.DemoPassword);

            await page.GotoAsync(
                $"{_fixture.PublicBaseUrl}{PlaywrightExecutiveCommandCenterHelper.CommandCenterPath}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await PlaywrightExecutiveCommandCenterHelper.AssertPermissionGateVisibleAsync(page);
            if (page.Url.Contains(PlaywrightExecutiveCommandCenterHelper.CommandCenterPath, StringComparison.OrdinalIgnoreCase))
            {
                await Assertions.Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
                {
                    NameRegex = new Regex("Back to dashboard|العودة", RegexOptions.IgnoreCase),
                })).ToBeVisibleAsync();
            }
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task ExecutiveCommandCenter_RegionalManager_LockedScopeAndScorecard()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            (playwright, browser, _, var page) =
                await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl, headless: true);

            await PlaywrightUiHelper.LoginViaUiAsync(
                page,
                _fixture.PublicBaseUrl,
                PlaywrightUiHelper.RegionalNorthEmail,
                PlaywrightUiHelper.DemoPassword);

            await PlaywrightExecutiveCommandCenterHelper.GotoCommandCenterAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightExecutiveCommandCenterHelper.AssertLockedScopeAsync(page);
            await PlaywrightExecutiveCommandCenterHelper.WaitForScorecardKpisAsync(page);

            await PlaywrightExecutiveCommandCenterHelper.ClickTabAsync(page, "cc-workforce", "Workforce");
            await PlaywrightExecutiveCommandCenterHelper.WaitForWorkforcePanelAsync(page);
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableFact]
    public async Task ExecutiveCommandCenter_BranchManager_LockedScopeAndOperations()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            (playwright, browser, _, var page) =
                await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl, headless: true);

            await PlaywrightUiHelper.LoginViaUiAsync(
                page,
                _fixture.PublicBaseUrl,
                PlaywrightUiHelper.BranchMezzehEmail,
                PlaywrightUiHelper.DemoPassword);

            await PlaywrightExecutiveCommandCenterHelper.GotoCommandCenterAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightExecutiveCommandCenterHelper.AssertLockedScopeAsync(page);

            await PlaywrightExecutiveCommandCenterHelper.ClickTabAsync(page, "cc-operations", "Operations");
            await PlaywrightExecutiveCommandCenterHelper.WaitForOperationsPanelAsync(page);
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [SkippableTheory]
    [MemberData(nameof(TabMemberData))]
    public async Task ExecutiveCommandCenter_GmTabSmoke(string tabId, string labelPattern, string panelHostId)
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            (playwright, browser, _, var page) =
                await PlaywrightUiHelper.LaunchPageAsync(_fixture.PublicBaseUrl, headless: true);

            await PlaywrightUiHelper.LoginViaUiAsync(
                page,
                _fixture.PublicBaseUrl,
                PlaywrightUiHelper.ExecutiveMisEmail,
                PlaywrightUiHelper.DemoPassword);

            await PlaywrightExecutiveCommandCenterHelper.GotoCommandCenterAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightExecutiveCommandCenterHelper.ClickTabAsync(page, tabId, labelPattern);

            await page.Locator($"#{panelHostId} .strategic-bento-card, #{panelHostId} .strategic-panel, #{panelHostId} table, #{panelHostId} .list-group-item")
                .First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 90_000,
                });
        }
        finally
        {
            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    public static IEnumerable<object[]> TabMemberData() =>
        PlaywrightExecutiveCommandCenterHelper.Tabs.Select(t => new object[]
        {
            t.TabId,
            t.LabelPattern,
            t.TabId switch
            {
                "cc-scorecard" => "executiveScorecardHost",
                "cc-network" => "executiveNetworkHost",
                "cc-mis" => "executiveMisHost",
                "cc-operations" => "executiveOperationsHost",
                "cc-workforce" => "executiveWorkforceHost",
                "cc-alerts" => "executiveAlertsHost",
                _ => "executiveScorecardHost",
            },
        });
}
