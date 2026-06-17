using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ASPNET.E2E.Tests.Infrastructure;

/// <summary>Playwright helpers for /Executive/CommandCenter (GM MIS dashboard).</summary>
public static class PlaywrightExecutiveCommandCenterHelper
{
    public const string CommandCenterPath = "/Executive/CommandCenter";

    public static readonly (string TabId, string LabelPattern)[] Tabs =
    [
        ("cc-scorecard", "Scorecard|نظرة سريعة"),
        ("cc-network", "Network|الشبكة"),
        ("cc-mis", "Financial MIS|MIS"),
        ("cc-operations", "Operations|العمليات"),
        ("cc-workforce", "Workforce|أداء الموظفين"),
        ("cc-alerts", "Alerts|التنبيهات"),
    ];

    public static async Task GotoCommandCenterAsync(IPage page, string baseUrl)
    {
        var summaryTask = page.WaitForResponseAsync(
            r => r.Url.Contains("/Telecom/GetExecutiveCommandCenterSummary", StringComparison.OrdinalIgnoreCase)
                 && r.Ok,
            new PageWaitForResponseOptions { Timeout = 90_000 });

        await page.GotoAsync(
            $"{baseUrl}{CommandCenterPath}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await summaryTask;
        await WaitForAppReadyAsync(page);
    }

    public static async Task WaitForAppReadyAsync(IPage page)
    {
        await page.Locator("#commandCenterApp").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
        await page.Locator(".executive-scope-bar").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
    }

    public static async Task AssertPermissionGateVisibleAsync(IPage page)
    {
        var onCommandCenter = page.Url.Contains(CommandCenterPath, StringComparison.OrdinalIgnoreCase);
        if (!onCommandCenter)
        {
            return;
        }

        await Assertions.Expect(page.Locator("#commandCenterGate")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Assertions.Expect(page.Locator("#commandCenterApp")).ToHaveClassAsync(new Regex("d-none"));
    }

    public static async Task ClickTabAsync(IPage page, string tabPaneId, string labelPattern)
    {
        var tab = page.Locator($"#ccTabs button[data-bs-target='#{tabPaneId}']");
        await tab.ClickAsync();
        await page.Locator($"#{tabPaneId}.tab-pane.active, #{tabPaneId}.tab-pane.show")
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
    }

    public static async Task ApplyScopeAsync(IPage page)
    {
        var summaryTask = page.WaitForResponseAsync(
            r => r.Url.Contains("/Telecom/GetExecutiveCommandCenterSummary", StringComparison.OrdinalIgnoreCase)
                 && r.Ok);

        await page.Locator(".executive-scope-bar .btn-strategic-gold").ClickAsync();
        await summaryTask;
    }

    public static async Task WaitForScorecardKpisAsync(IPage page)
    {
        var host = page.Locator("#executiveScorecardHost");
        await host.Locator(".strategic-bento-card").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
        var cards = host.Locator(".strategic-bento-card");
        var count = await cards.CountAsync();
        if (count < 6)
        {
            throw new InvalidOperationException($"Expected at least 6 scorecard KPI cards, found {count}.");
        }
    }

    public static async Task WaitForNetworkPanelAsync(IPage page)
    {
        var host = page.Locator("#executiveNetworkHost");
        await host.GetByText(new Regex("Demo|محاكاة", RegexOptions.IgnoreCase)).First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await host.Locator(".strategic-bento-card, .strategic-panel").First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public static async Task WaitForMisPanelAsync(IPage page)
    {
        var host = page.Locator("#executiveMisHost");
        await host.Locator(".strategic-bento-card").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
        await page.Locator("#executiveGeoHost .geo-heatmap-canvas, #executiveGeoHost .strategic-panel")
            .First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    public static async Task WaitForOperationsPanelAsync(IPage page)
    {
        var host = page.Locator("#executiveOperationsHost");
        await host.GetByText(new Regex("Read-only|read-only|مراقبة", RegexOptions.IgnoreCase)).First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await host.Locator(".strategic-bento-card").First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public static async Task WaitForWorkforcePanelAsync(IPage page)
    {
        var host = page.Locator("#executiveWorkforceHost");
        await host.Locator("table tbody tr").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
    }

    public static async Task OpenFirstWorkforceDetailAsync(IPage page)
    {
        var host = page.Locator("#executiveWorkforceHost");
        await host.Locator("table tbody tr button").First.ClickAsync();
        await host.Locator(".strategic-panel .badge, .strategic-panel .list-group-item").First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    public static async Task WaitForAlertsPanelAsync(IPage page)
    {
        var host = page.Locator("#executiveAlertsHost");
        await host.Locator(".list-group-item, .nav-tabs").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
    }

    public static async Task SwitchAlertsSupervisorSubTabAsync(IPage page)
    {
        var host = page.Locator("#executiveAlertsHost");
        await host.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Supervisor|المشرف", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await host.Locator(".list-group-item").First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    public static async Task AssertGmScopeFiltersVisibleAsync(IPage page)
    {
        var scope = page.Locator(".executive-scope-bar");
        await Assertions.Expect(scope.Locator("select.form-select").First).ToBeVisibleAsync();
        await Assertions.Expect(scope.Locator("input[type='date']").First).ToBeVisibleAsync();
        await Assertions.Expect(scope.Locator(".btn-strategic-gold")).ToBeVisibleAsync();
    }

    public static async Task AssertLockedScopeAsync(IPage page)
    {
        var scope = page.Locator(".executive-scope-bar");
        await Assertions.Expect(scope.GetByText(new Regex("Locked scope|نطاق مقفل", RegexOptions.IgnoreCase)))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        await Assertions.Expect(scope.Locator("select.form-select")).ToHaveCountAsync(0);
    }
}
