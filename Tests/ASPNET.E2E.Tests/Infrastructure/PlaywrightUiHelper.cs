using Microsoft.Playwright;

namespace ASPNET.E2E.Tests.Infrastructure;

public static class PlaywrightUiHelper
{
    public const string AdminEmail = "admin@root.com";
    public const string AdminPassword = "123456";
    public const string ShowroomEmail = "st-showroom@syriatelecom-demo.local";
    public const string CallCenterEmail = "st-callcenter@syriatelecom-demo.local";
    public const string DemoPassword = "123456";

    private const int PresentationSlowMoMs = 2_000;

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

    public static async Task<(IPlaywright Playwright, IBrowser Browser, IBrowserContext Context, IPage Page)> LaunchPageAsync(
        string baseUrl,
        bool headless = true,
        int? slowMo = null)
    {
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
        await page.Locator("[data-testid='list-act-effective-mode'], #C360NewLineModal, .customer-360-premium")
            .First
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
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
