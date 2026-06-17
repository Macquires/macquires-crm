using Microsoft.Playwright;

namespace ASPNET.E2E.Tests.Infrastructure;

public static class PlaywrightBssWizardHelper
{
    public static async Task SmokeHubWizardAsync(IPage page, string hubTile, string anchorTestId)
    {
        await PlaywrightUiHelper.OpenHubWizardAsync(page, hubTile);
        await Assertions.Expect(page.Locator($"[data-testid='{anchorTestId}']")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        await CloseHubWizardAsync(page);
    }

    public static async Task SmokeC360WizardAsync(
        IPage page,
        string baseUrl,
        string customerId,
        string wizardKind,
        string anchorTestId,
        string? lineKey = null)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, baseUrl, customerId, wizardKind, lineKey);
        await Assertions.Expect(page.Locator($"[data-testid='{anchorTestId}']")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    public static async Task SmokeListWizardAsync(
        IPage page,
        string msisdn,
        string lineAction,
        string anchorTestId,
        string? modalSelector = null)
    {
        await OpenListLineActionAsync(page, msisdn, lineAction);
        await Assertions.Expect(page.Locator($"[data-testid='{anchorTestId}']")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        await CloseListModalAsync(page, modalSelector);
    }

    public static async Task OpenListLineActionAsync(IPage page, string msisdn, string actionName)
    {
        var card = page.Locator(".customer-360-asset-card").Filter(new LocatorFilterOptions { HasText = msisdn });
        await card.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await card.First.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Line actions" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = actionName }).First.ClickAsync();
    }

    public static async Task CloseHubWizardAsync(IPage page)
    {
        var panel = page.Locator("#telecomOpsWizardPanel");
        if (!await panel.IsVisibleAsync())
        {
            return;
        }

        var close = panel.Locator(".btn-close").First;
        if (await close.CountAsync() > 0)
        {
            await close.ClickAsync();
        }
        else
        {
            await page.Keyboard.PressAsync("Escape");
        }

        try
        {
            await panel.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 10_000,
            });
        }
        catch (TimeoutException)
        {
            await page.GotoAsync(page.Url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        }
    }

    public static async Task CloseListModalAsync(IPage page, string? modalSelector = null)
    {
        var modal = !string.IsNullOrWhiteSpace(modalSelector)
            ? page.Locator(modalSelector)
            : page.Locator(".modal.show").Last;

        if (await modal.CountAsync() > 0 && await modal.IsVisibleAsync())
        {
            var close = modal.Locator(".btn-close").First;
            if (await close.CountAsync() > 0)
            {
                await close.ClickAsync();
            }
            else
            {
                await page.Keyboard.PressAsync("Escape");
            }

            await modal.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 30_000,
            });
        }
    }

    public static async Task CloseListHeaderModalAsync(IPage page, string modalSelector)
    {
        var modal = page.Locator(modalSelector);
        if (await modal.IsVisibleAsync())
        {
            await modal.Locator(".btn-close").First.ClickAsync();
            await modal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
        }
    }

    public static async Task RunC360TimelineFiltersAsync(IPage page, IEnumerable<string> filterLabels)
    {
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Timeline");
        foreach (var label in filterLabels)
        {
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = label }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    public static async Task ClickC360WizardNextAsync(IPage page)
    {
        await page.Locator("#c360ProvWizardModal")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
            {
                NameRegex = new System.Text.RegularExpressions.Regex("Next|Registration|التالي", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            })
            .ClickAsync();
    }

    public static async Task CreateC360WizardDraftAsync(IPage page)
    {
        await page.Locator("#c360ProvWizardModal")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
            {
                NameRegex = new System.Text.RegularExpressions.Regex("Save request|Create draft|حفظ", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            })
            .ClickAsync();
        await page.Locator("#c360ProvWizardModal").Locator("text=/OP-/").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
    }

    public static async Task SelectC360LineByMsisdnAsync(IPage page, string msisdn)
    {
        var value = await page.EvaluateAsync<string>(
            """
            (digits) => {
                for (const sel of document.querySelectorAll('select.form-select')) {
                    for (const opt of sel.options) {
                        if ((opt.textContent || '').includes(digits)) return opt.value;
                    }
                }
                return '';
            }
            """,
            msisdn);
        if (!string.IsNullOrWhiteSpace(value))
        {
            await page.Locator("select.form-select").First.SelectOptionAsync(value);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
