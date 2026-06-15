using System.Text;
using System.Text.RegularExpressions;
using Application.Common.CQS.Queries;
using Application.Common.Telecom.Suspension;
using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.UI.LiveDemo;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.UI.LiveDemo;

/// <summary>
/// Headed, slow-motion executive presentation suite for live telecom BSS demos.
/// Run: dotnet test --filter "FullyQualifiedName~TelecomBssLiveExecutiveDemoTests"
/// </summary>
[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "LiveDemo")]
public sealed class TelecomBssLiveExecutiveDemoTests
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomBssLiveExecutiveDemoTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExecutiveDemo_RunAllScenarios_Sequentially()
    {
        await LiveDemoInfrastructureGuard.VerifyOrThrowAsync(_fixture);

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        var activationSeed = await E2ETestDataHelper.ResolveActivationSeedAsync(_fixture.AppFactory.Services);
        var suspensionTarget = await ResolveActiveSuspensionTargetAsync(_fixture.AppFactory.Services);
        var customerSearchTerm = await ResolveCustomerSearchTermAsync(
            _fixture.AppFactory.Services,
            activationSeed.CustomerId);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        var kycPaths = new List<string>();

        try
        {
            (playwright, browser, _, var page) =
                await PlaywrightUiHelper.LaunchPresentationPageAsync(_fixture.PublicBaseUrl);

            await RunScenario1ActivationAsync(page, activationSeed, customerSearchTerm, kycPaths);
            await RunScenario2FraudSuspensionAsync(page, suspensionTarget, kycPaths);
            await RunScenario3SupportTicketTimelineAsync(page, activationSeed.CustomerId);
        }
        finally
        {
            foreach (var path in kycPaths.Where(File.Exists))
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // Best-effort cleanup for temp presentation fixtures.
                }
            }

            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    private async Task RunScenario1ActivationAsync(
        IPage page,
        ActivationSeedBundle seed,
        string customerSearchTerm,
        ICollection<string> kycPaths)
    {
        LiveDemoConsole.LogScenarioStart(
            "▶️ SCENARIO 1: Simulating a Showroom Agent initiating a brand new Subscriber Line Activation (ACT)...");

        LiveDemoConsole.LogStep("Authenticating showroom POS agent and opening the Telecom Operations Hub.");
        await PlaywrightUiHelper.LoginViaUiAsync(
            page,
            _fixture.PublicBaseUrl,
            PlaywrightUiHelper.ShowroomEmail,
            PlaywrightUiHelper.DemoPassword);
        await PlaywrightUiHelper.GotoTelecomHubAsync(page, _fixture.PublicBaseUrl);

        LiveDemoConsole.LogStep("Launching the New Line Activation (ACT) wizard from the hub command tiles.");
        await page.Locator(".tile-activate").ClickAsync();
        await page.Locator("#telecomOpsWizardPanel").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
        });

        var wizard = page.Locator("#telecomOpsWizardPanel");
        LiveDemoConsole.LogStep("Binding the subscriber profile and reserving a clean MSISDN from the inventory pool.");
        await wizard.Locator("input[type='search']").First.FillAsync(customerSearchTerm);
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Search" }).First.ClickAsync();
        await wizard.Locator(".list-group-item-action").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
        });
        await wizard.Locator(".list-group-item-action").First.ClickAsync();

        await page.Locator("#wizActivationLineType").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
        });
        await page.Locator("#wizActivationLineType option").Nth(1).WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
        });
        await page.Locator("#wizActivationLineType").SelectOptionAsync(new SelectOptionValue { Index = 1 });

        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new Regex("Browse", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await page.Locator(".msisdn-picker-row").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var msisdnRow = page.Locator(".msisdn-picker-row").Filter(new LocatorFilterOptions
        {
            HasText = seed.Msisdn,
        });
        if (await msisdnRow.CountAsync() == 0)
        {
            msisdnRow = page.Locator(".msisdn-picker-row").First;
        }

        await msisdnRow.Locator("button.btn-danger").ClickAsync();
        await Assertions.Expect(wizard.Locator(".alert-success").Filter(new LocatorFilterOptions { HasText = seed.Msisdn }))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        LiveDemoConsole.LogStep("Assigning the standard prepaid package plan (YA_HALA_30).");
        await page.Locator("#wizTargetOffer").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
        });
        var offeringOption = page.Locator("#wizTargetOffer option").Filter(new LocatorFilterOptions
        {
            HasText = "YA_HALA_30",
        });
        await offeringOption.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        var offeringValue = await offeringOption.First.GetAttributeAsync("value")
            ?? throw new InvalidOperationException("YA_HALA_30 offering option not found.");
        await page.Locator("#wizTargetOffer").SelectOptionAsync(offeringValue);

        LiveDemoConsole.LogStep("Advancing to registration — uploading KYC identity marker and creating the CBS draft.");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new Regex("Next", RegexOptions.IgnoreCase) })
            .ClickAsync();

        var kycPath = CreateTempKycPdf();
        kycPaths.Add(kycPath);
        await wizard.Locator(".kyc-dropzone input[type='file']").SetInputFilesAsync(kycPath);
        await wizard.Locator(".kyc-dropzone--locked").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new Regex("Save request", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await wizard.Locator("text=/OP-/").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        LiveDemoConsole.LogStep("Confirming CBS/HLR provisioning on the network integration layer.");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new Regex("Confirm CBS", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await wizard.Locator("text=/Confirmed|success|provision/i").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        LiveDemoConsole.LogSuccess(
            "✅ SUCCESS: Core transaction confirmed. Subscriber profile and MSISDN status synchronized on the CBS/HLR network layers!");
    }

    private async Task RunScenario2FraudSuspensionAsync(
        IPage page,
        SuspensionTarget target,
        ICollection<string> kycPaths)
    {
        LiveDemoConsole.LogScenarioStart(
            "▶️ SCENARIO 2: Revenue Assurance framework detecting suspicious activity and triggering a Fraud/Regulatory Suspension (SUS)...");

        LiveDemoConsole.LogStep("Re-authenticating as showroom agent and opening Customer List for the active subscriber.");
        await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page,
            _fixture.PublicBaseUrl,
            PlaywrightUiHelper.ShowroomEmail,
            PlaywrightUiHelper.DemoPassword);
        await PlaywrightUiHelper.GotoCustomerListWithCustomerAsync(page, _fixture.PublicBaseUrl, target.CustomerId);

        LiveDemoConsole.LogStep("Opening the Suspension (SUS) command module for the target line.");
        var lineRow = page.Locator(".customer-360-premium").Locator("tr").Filter(new LocatorFilterOptions
        {
            HasText = target.Msisdn,
        }).First;
        await lineRow.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new Regex("Line actions", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await lineRow.GetByRole(AriaRole.Menuitem, new LocatorGetByRoleOptions { NameRegex = new Regex("Suspend", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await page.Locator("#C360SuspensionModal").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
        });

        var modal = page.Locator("#C360SuspensionModal");
        LiveDemoConsole.LogStep("Classifying the suspension as Fraud/Regulatory and validating compliance guardrails.");
        await modal.Locator("select").First.SelectOptionAsync(SuspensionWellKnown.Fraud);

        await Assertions.Expect(modal.Locator("#susSupervisorList")).ToBeVisibleAsync();
        await Assertions.Expect(modal.GetByText(new Regex("KYC document", RegexOptions.IgnoreCase))).ToBeVisibleAsync();
        await Assertions.Expect(modal.Locator(".alert-warning")).ToBeVisibleAsync();

        var submitButton = modal.Locator(".modal-footer .btn-danger");
        await Assertions.Expect(submitButton).ToContainTextAsync(new Regex("Back office", RegexOptions.IgnoreCase));
        await Assertions.Expect(submitButton).Not.ToContainTextAsync(new Regex("^Confirm$", RegexOptions.IgnoreCase));

        await modal.Locator("input[placeholder*='Reason'], input[placeholder*='reason']")
            .Or(modal.Locator("input").Nth(2))
            .FillAsync("Executive demo — suspected SIM box fraud pattern");
        await modal.Locator("input[placeholder*='Ticket'], input[dir='ltr']").First
            .FillAsync("TT-2026-DEMO");
        await modal.Locator("#susSupervisorList").CheckAsync();

        var identityPath = CreateTempKycPdf();
        kycPaths.Add(identityPath);
        await modal.Locator("input[type='file']").Last.SetInputFilesAsync(identityPath);

        LiveDemoConsole.LogStep("Dispatching the suspension to the Back Office queue for supervisor evaluation.");
        await submitButton.ClickAsync();
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);
        await modal.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 60_000 });

        using (var scope = _fixture.AppFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var latest = await db.TelecomOperationRequest.AsNoTracking()
                .Where(o => !o.IsDeleted
                            && o.SubscriberProfileId == target.SubscriberProfileId
                            && o.Kind == TelecomOperationKind.TemporarySuspension)
                .OrderByDescending(o => o.CreatedAtUtc)
                .FirstAsync();

            Assert.Equal(SuspensionWellKnown.Fraud, latest.SuspensionType);
            Assert.Equal("BackOffice", latest.ApprovalLevelRequired);
        }

        LiveDemoConsole.LogCompliance(
            "🔒 COMPLIANCE SECURED: Immediate activation blocked. Operation safely rerouted to the Back Office Queue for supervisor evaluation.");
    }

    private async Task RunScenario3SupportTicketTimelineAsync(IPage page, string customerId)
    {
        LiveDemoConsole.LogScenarioStart(
            "▶️ SCENARIO 3: Call Center Agent opening a formal Technical Support Ticket (SUP) for data speed degradation...");

        LiveDemoConsole.LogStep("Authenticating call center agent and loading the Customer 360 command profile.");
        await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page,
            _fixture.PublicBaseUrl,
            PlaywrightUiHelper.CallCenterEmail,
            PlaywrightUiHelper.DemoPassword);

        await page.GotoAsync(
            $"{_fixture.PublicBaseUrl}/Telecom/Customer360Profile?customerId={Uri.EscapeDataString(customerId)}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#c360-page").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        LiveDemoConsole.LogStep("Launching the Support Ticket (SUP) wizard and logging a network degradation case.");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameRegex = new Regex("Support ticket", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await page.Locator("[data-testid='c360-support-wizard']").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
        });

        await page.Locator("[data-testid='c360-support-issue-type']").SelectOptionAsync("0");
        await page.Locator("[data-testid='c360-support-wizard'] textarea")
            .FillAsync("Network degradation — low mobile data speed reported in Mazzeh sector. Customer reports <1 Mbps on LTE.");

        await page.Locator("#c360ProvWizardModal")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new Regex("Create", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await page.Locator("#c360ProvWizardModal").Locator("strong").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        var ticketNumber = (await page.Locator("#c360ProvWizardModal").Locator("strong").First.InnerTextAsync()).Trim();

        await page.Locator("#c360ProvWizardModal")
            .GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { NameRegex = new Regex("Close", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        LiveDemoConsole.LogStep("Verifying the ticket is interleaved into the Customer 360 unified timeline.");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameRegex = new Regex("Unified timeline|timeline", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameRegex = new Regex("Tickets|تذاكر", RegexOptions.IgnoreCase) })
            .ClickAsync();

        var timelineEntry = page.Locator(".list-group-item").Filter(new LocatorFilterOptions
        {
            HasText = ticketNumber,
        });
        await timelineEntry.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var statusText = await timelineEntry.First.InnerTextAsync();
        Assert.Matches(new Regex("Open|In progress", RegexOptions.IgnoreCase), statusText);

        LiveDemoConsole.LogSuccess(
            "✅ TIMELINE VERIFIED: Technical ticket successfully generated, captured in the Customer 360 audit stream, and dispatched to Tier-2 network engineering!");
    }

    private static string CreateTempKycPdf()
    {
        var path = Path.Combine(Path.GetTempPath(), $"live-demo-kyc-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, "%PDF-1.4 Live Demo KYC Placeholder", Encoding.UTF8);
        return path;
    }

    private static async Task<string> ResolveCustomerSearchTermAsync(
        IServiceProvider services,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var term = await query.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Id == customerId)
            .Select(c => c.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(term) ? customerId : term;
    }

    private static async Task<SuspensionTarget> ResolveActiveSuspensionTargetAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();

        var row = await query.TelecomSubscription.AsNoTracking()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfile != null
                        && !s.SubscriberProfile.IsDeleted
                        && s.SubscriberProfile.OperationalStatus == SubscriberOperationalStatus.Active
                        && s.MsisdnAsset != null
                        && !s.MsisdnAsset.IsDeleted)
            .OrderBy(s => s.CreatedAtUtc)
            .Select(s => new SuspensionTarget(
                s.SubscriberProfile!.CustomerId,
                s.SubscriberProfileId,
                s.MsisdnAsset!.Msisdn))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active subscriber line found for SUS demo.");

        return row;
    }

    private sealed record SuspensionTarget(string CustomerId, string SubscriberProfileId, string Msisdn);
}
