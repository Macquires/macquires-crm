using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.Infrastructure;

/// <summary>
/// Headed demo pacing — showcase moments are visible; routine steps stay snappy and consistent.
/// </summary>
public static class PlaywrightDemoPresentationHelper
{
    private const int MouseStepsShowcase = 8;
    private const int StepDelayMs = 20;
    private const int TypingDelayMs = 55;
    private const int TypingTailMs = 250;
    private const int RoutinePauseMs = 180;
    private const int ShowcasePauseMs = 450;
    /// <summary>Hold SweetAlert on screen so the audience can read it (not extra pauses everywhere).</summary>
    private const int SwalReadHoldMs = 1_200;

    public static bool IsPresentationMode() =>
        !PlaywrightUiHelper.ResolveHeadless(true)
        || PlaywrightUiHelper.ResolvePresentationSlowMoMs() > 0;

    public static async Task PauseRoutineAsync(IPage page)
    {
        if (IsPresentationMode())
        {
            await page.WaitForTimeoutAsync(RoutinePauseMs);
        }
    }

    public static async Task PauseShowcaseAsync(IPage page)
    {
        if (IsPresentationMode())
        {
            await page.WaitForTimeoutAsync(ShowcasePauseMs);
        }
    }

    public static void EnsurePageOpen(IPage page)
    {
        if (page.IsClosed)
        {
            throw new InvalidOperationException(
                "Chromium was closed before the step finished. Keep the browser open until the test completes.");
        }
    }

    public static async Task MoveMouseToLocatorAsync(IPage page, ILocator locator, bool showcase = true)
    {
        if (!IsPresentationMode() || !showcase)
        {
            return;
        }

        EnsurePageOpen(page);

        try
        {
            await locator.ScrollIntoViewIfNeededAsync();
            var box = await locator.BoundingBoxAsync();
            if (box is null)
            {
                return;
            }

            var targetX = box.X + box.Width / 2;
            var targetY = box.Y + box.Height / 2;
            await page.Mouse.MoveAsync(8, 8);

            for (var step = 1; step <= MouseStepsShowcase; step++)
            {
                var x = 8 + (targetX - 8) * step / MouseStepsShowcase;
                var y = 8 + (targetY - 8) * step / MouseStepsShowcase;
                await page.Mouse.MoveAsync(x, y);
                await Task.Delay(StepDelayMs);
            }
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("closed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chromium closed during mouse move.", ex);
        }
    }

    /// <summary>Fast click for pickers, dropdowns, modal rows — no mouse travel.</summary>
    public static async Task ClickRoutineAsync(IPage page, ILocator locator)
    {
        EnsurePageOpen(page);
        await locator.ScrollIntoViewIfNeededAsync();
        await locator.ClickAsync();
        await PauseRoutineAsync(page);
    }

    /// <summary>Visible mouse + short pause for key demo beats (validation, KYC, confirm).</summary>
    public static async Task ClickShowcaseAsync(IPage page, ILocator locator)
    {
        EnsurePageOpen(page);
        await MoveMouseToLocatorAsync(page, locator, showcase: true);
        await locator.ClickAsync();
        await PauseShowcaseAsync(page);
    }

    public static Task ClickWithPresentationAsync(IPage page, ILocator locator) =>
        ClickShowcaseAsync(page, locator);

    public static async Task TypeSlowlyAsync(ILocator locator, string text)
    {
        await locator.ClickAsync();
        await locator.FillAsync(string.Empty);
        if (!IsPresentationMode())
        {
            await locator.FillAsync(text);
            return;
        }

        await locator.PressSequentiallyAsync(text, new LocatorPressSequentiallyOptions { Delay = TypingDelayMs });
        await Task.Delay(TypingTailMs);
    }

    public static async Task SelectOptionRoutineAsync(IPage page, ILocator select, SelectOptionValue value)
    {
        await select.ScrollIntoViewIfNeededAsync();
        await select.SelectOptionAsync(value);
        await PauseRoutineAsync(page);
    }

    public static Task SelectOptionWithPresentationAsync(IPage page, ILocator select, SelectOptionValue value) =>
        SelectOptionRoutineAsync(page, select, value);

    public static async Task AssertSwalAsync(IPage page, string titlePattern, string bodyPattern)
    {
        var swal = page.Locator(".swal2-container");
        await swal.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000,
        });

        var title = await swal.Locator(".swal2-title").InnerTextAsync();
        var body = await swal.Locator("#swal2-html-container, .swal2-html-container").InnerTextAsync();

        Assert.Matches(new Regex(titlePattern, RegexOptions.IgnoreCase), title);
        Assert.Matches(new Regex(bodyPattern, RegexOptions.IgnoreCase), body);

        if (IsPresentationMode())
        {
            await page.WaitForTimeoutAsync(SwalReadHoldMs);
        }
    }

    public static async Task DismissSwalOkAsync(IPage page)
    {
        var confirm = page.Locator(".swal2-container").GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("OK|Ok|حسنا|موافق", RegexOptions.IgnoreCase),
        });
        await ClickRoutineAsync(page, confirm.First);
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);
    }
}
