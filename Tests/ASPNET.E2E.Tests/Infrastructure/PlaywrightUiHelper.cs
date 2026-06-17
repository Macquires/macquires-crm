using Microsoft.Playwright;

namespace ASPNET.E2E.Tests.Infrastructure;

public static class PlaywrightUiHelper
{
    public const string AdminEmail = "admin@root.com";
    public const string AdminPassword = "123456";
    public const string ShowroomEmail = "st-showroom@syriatelecom-demo.local";
    public const string CallCenterEmail = "st-callcenter@syriatelecom-demo.local";
    public const string ExecutiveMisEmail = "st-mis@syriatelecom-demo.local";
    public const string RegionalNorthEmail = "st-regional-north@syriatelecom-demo.local";
    public const string BranchMezzehEmail = "st-branch-mezzeh@syriatelecom-demo.local";
    public const string DemoPassword = "123456";

    private const int PresentationSlowMoMs = 2_000;
    public const int ComprehensiveMasterSlowMoMs = 1_500;

    public static async Task EnsureChromiumInstalledAsync()
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright browser install failed with exit code {exitCode}.");
        }
    }

    public static Task<(IPlaywright Playwright, IBrowser Browser, IBrowserContext Context, IPage Page)> LaunchPresentationPageAsync(
        string baseUrl) =>
        LaunchPageAsync(baseUrl, headless: false, slowMo: PresentationSlowMoMs);

    public static Task<(IPlaywright Playwright, IBrowser Browser, IBrowserContext Context, IPage Page)> LaunchComprehensiveMasterPageAsync(
        string baseUrl) =>
        LaunchPageAsync(baseUrl, headless: false, slowMo: ComprehensiveMasterSlowMoMs);

    public static bool ResolveHeadless(bool headless = true)
    {
        var env = Environment.GetEnvironmentVariable("E2E_HEADLESS");
        if (string.IsNullOrWhiteSpace(env))
        {
            return headless;
        }

        return !(env.Equals("false", StringComparison.OrdinalIgnoreCase) || env == "0");
    }

    public static async Task<(IPlaywright Playwright, IBrowser Browser, IBrowserContext Context, IPage Page)> LaunchPageAsync(
        string baseUrl,
        bool headless = true,
        int? slowMo = null)
    {
        headless = ResolveHeadless(headless);
        Console.WriteLine($"[E2E] Launching Chromium (headless={headless})…");

        var playwright = await Playwright.CreateAsync();
        var launchOptions = new BrowserTypeLaunchOptions { Headless = headless };
        if (slowMo is > 0)
        {
            launchOptions.SlowMo = slowMo.Value;
        }

        var browser = await playwright.Chromium.LaunchAsync(launchOptions);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "en-US",
        });
        await context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = ".AspNetCore.Culture",
                Value = "c=en|uic=en",
                Url = baseUrl,
            },
        ]);

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);
        return (playwright, browser, context, page);
    }

    public static Task LoginViaUiAsync(IPage page, string baseUrl, CancellationToken cancellationToken = default) =>
        LoginViaUiAsync(page, baseUrl, AdminEmail, AdminPassword, cancellationToken);

    public static async Task LoginViaUiAsync(
        IPage page,
        string baseUrl,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await EnsureCultureCookieAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}/Accounts/Login", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await page.Locator("#Email").FillAsync(email);
        await page.Locator("#Password").FillAsync(password);
        await page.Locator("button[type='submit']").ClickAsync();

        await page.WaitForURLAsync(
            url => !url.Contains("/Accounts/Login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 60_000 });

        cancellationToken.ThrowIfCancellationRequested();
    }

    public static async Task LogoutViaUiAsync(IPage page, string baseUrl)
    {
        await page.Context.ClearCookiesAsync();
        await EnsureCultureCookieAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}/Accounts/Login", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public static async Task WaitForSwalToCloseAsync(IPage page)
    {
        var swal = page.Locator(".swal2-container");
        if (await swal.CountAsync() > 0)
        {
            await swal.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 60_000,
            });
        }
    }

    private static async Task EnsureCultureCookieAsync(IPage page, string baseUrl)
    {
        await page.Context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = ".AspNetCore.Culture",
                Value = "c=en|uic=en",
                Url = baseUrl,
            },
        ]);
    }

    public static async Task GotoTelecomHubAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/Telecom/TelecomHub", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#telecomSearchInput").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public static async Task GotoCustomerListWithCustomerAsync(IPage page, string baseUrl, string customerId)
    {
        await page.GotoAsync(
            $"{baseUrl}/Customers/CustomerList?customerId={Uri.EscapeDataString(customerId)}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".customer-360-premium")
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    public static async Task OpenListLineActionForMsisdnAsync(IPage page, string msisdn, string actionName)
    {
        var card = page.Locator(".customer-360-asset-card").Filter(new LocatorFilterOptions { HasText = msisdn });
        await card.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await card.First.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Line actions" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = actionName }).First.ClickAsync();
    }

    public static async Task OpenHubWizardAsync(IPage page, string tileKind)
    {
        await page.Locator($"[data-testid='hub-wizard-tile-{tileKind}']").ClickAsync();
        await page.Locator("#telecomOpsWizardPanel").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public static async Task GotoC360ProfileAsync(IPage page, string baseUrl, string customerId)
    {
        await page.GotoAsync(
            $"{baseUrl}/Telecom/Customer360Profile?customerId={Uri.EscapeDataString(customerId)}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#c360-page").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
    }

    public static async Task ClickC360TabAsync(IPage page, string tabNamePattern)
    {
        await page.Locator(".c360-tab-btn").Filter(new LocatorFilterOptions
        {
            HasText = tabNamePattern,
        }).First.ClickAsync();
    }

    public static async Task InstallExecutiveAlertsSnifferAsync(IPage page, string baseUrl)
    {
        await page.EvaluateAsync(
            """
            async (origin) => {
                window.__e2eExecutiveAlerts = window.__e2eExecutiveAlerts || [];
                if (!window.signalR) {
                    await new Promise((resolve, reject) => {
                        const script = document.createElement('script');
                        script.src = 'https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.7/dist/browser/signalr.min.js';
                        script.onload = resolve;
                        script.onerror = reject;
                        document.head.appendChild(script);
                    });
                }
                if (window.__e2eExecutiveSignalR) {
                    return;
                }
                const token =
                    (window.StorageManager && (
                        window.StorageManager.getAccessToken?.()
                        || window.StorageManager.getToken?.()
                    )) || '';
                const connection = new signalR.HubConnectionBuilder()
                    .withUrl(origin + '/hubs/executive-alerts', {
                        accessTokenFactory: () => token,
                    })
                    .withAutomaticReconnect()
                    .build();
                connection.on('executiveAlert', (payload) => {
                    window.__e2eExecutiveAlerts.push(payload);
                });
                try {
                    await connection.start();
                    if (connection.invoke) {
                        try { await connection.invoke('JoinExecutiveMis'); } catch { /* group join optional */ }
                    }
                    window.__e2eExecutiveSignalR = connection;
                } catch {
                    // SignalR is optional for E2E flow validation; don't fail the suite on hub bootstrap issues.
                }
            }
            """,
            baseUrl.TrimEnd('/'));
    }

    public static async Task<int> ReadExecutiveAlertCountAsync(IPage page) =>
        await page.EvaluateAsync<int>("() => (window.__e2eExecutiveAlerts || []).length");

    public static async Task CompleteWalletRechargeSwalAsync(IPage page, decimal amount, string gatewayReference)
    {
        var swal = page.Locator(".swal2-container");
        await swal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        var continueBtn = swal.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new System.Text.RegularExpressions.Regex("Continue|Next|متابعة", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });
        if (await continueBtn.CountAsync() > 0)
        {
            await continueBtn.First.ClickAsync();
        }

        await swal.Locator("input[type='number']").FillAsync(amount.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
        await swal.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new System.Text.RegularExpressions.Regex("Continue|Next|متابعة", System.Text.RegularExpressions.RegexOptions.IgnoreCase) })
            .First.ClickAsync();

        await swal.Locator("input[type='text']").FillAsync(gatewayReference);
        await swal.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new System.Text.RegularExpressions.Regex("Confirm|Recharge|تأكيد|شحن", System.Text.RegularExpressions.RegexOptions.IgnoreCase) })
            .First.ClickAsync();

        await swal.Locator(".swal2-success").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });
    }

    public static async Task CloseC360WizardAsync(IPage page)
    {
        var modal = page.Locator("#c360ProvWizardModal");
        if (await modal.IsVisibleAsync())
        {
            await modal.Locator(".btn-close").ClickAsync();
            await modal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
        }
    }

    public static async Task SelectC360LineByMsisdnAsync(IPage page, string msisdn)
    {
        var select = page.Locator("select.form-select").Filter(new LocatorFilterOptions
        {
            Has = page.Locator($"option:has-text('{msisdn}')"),
        });
        if (await select.CountAsync() == 0)
        {
            select = page.Locator("select.form-select").First;
        }

        await select.SelectOptionAsync(new SelectOptionValue { Label = msisdn });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public static async Task GotoC360WizardAsync(
        IPage page,
        string baseUrl,
        string customerId,
        string wizardKind,
        string? lineKey = null)
    {
        var url =
            $"{baseUrl}/Telecom/Customer360Profile?customerId={Uri.EscapeDataString(customerId)}&wizard={Uri.EscapeDataString(wizardKind)}";
        if (!string.IsNullOrWhiteSpace(lineKey))
        {
            url += $"&lineKey={Uri.EscapeDataString(lineKey)}";
        }

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#c360ProvWizardModal").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
    }

    public static async Task OpenChangeNumberWizardAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Open change number wizard" })
            .ClickAsync();
        await page.Locator("#telecomOpsWizardPanel").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public static async Task DisposeAsync(IPlaywright? playwright, IBrowser? browser)
    {
        if (browser is not null)
        {
            await browser.DisposeAsync();
        }

        playwright?.Dispose();
    }
}
