using System.Text;
using System.Text.RegularExpressions;
using Application.Common.Telecom;
using Application.Common.Telecom.Suspension;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.Infrastructure;

/// <summary>
/// Orchestrates the 4-phase Sovereign Master Matrix journey per wizard:
/// POS Hub PBAC → Back-Office SSOT → Customer 360 timeline → Executive Command Center pulse.
/// </summary>
public static class PlaywrightMasterMatrixHelper
{
    private const decimal RechargeAmount = 3_000m;
    private const string VasServiceCode = "VAS_CALLER_ID";

    private static readonly string[] FrontlineTileCodes = ["ACT", "SIM", "CNR", "MGR", "PAY"];
    private static readonly string[] BackOfficeTileCodes = ["CGT", "TKO", "TRM", "SUS", "BDR"];
    private static readonly string[] SupervisorTileCodes = ["RCN", "RFD", "DEV", "VAS", "SUP"];

    public sealed record MasterMatrixSubmitResult(
        string CustomerId,
        string? SubscriberProfileId,
        string? OperationId,
        string? OperationNumber,
        string? GatewayReference,
        string? TicketNumber,
        string TimelineMarker);

    public sealed record MasterMatrixWizard(
        string Code,
        string HubTileKind,
        bool IsRechargeTile,
        string HubPersonaEmail,
        string HubAnchorTestId,
        string C360WizardKind,
        TelecomOperationKind? OperationKind,
        bool RequiresApproval,
        string? ApprovalScenarioKey,
        string TimelineFilterLabel,
        string DemoMsisdn);

    public static IReadOnlyList<MasterMatrixWizard> AllWizards { get; } =
    [
        new("ACT", "activate", false, PlaywrightUiHelper.ShowroomEmail, "hub-act-line-type", "activate",
            TelecomOperationKind.NewActivation, false, null, "Operations", TelecomDemoMsisdn.ShowcaseHealthy),
        new("MGR", "migrate", false, PlaywrightUiHelper.ShowroomEmail, "hub-mgr-offering-select", "migrate",
            TelecomOperationKind.Migration, false, null, "Operations", TelecomDemoMsisdn.ShowcaseHealthy),
        new("SIM", "simswap", false, PlaywrightUiHelper.ShowroomEmail, "hub-sim-replacement-reason", "simswap",
            TelecomOperationKind.SimSwap, false, null, "Operations", TelecomDemoMsisdn.ShowcaseNotProvisioned),
        new("CNR", "changeNumber", false, PlaywrightUiHelper.ShowroomEmail, "telecom-cn-change-mode", "changeNumber",
            TelecomOperationKind.NumberPortability, true, "bo-cnr-premium", "Operations", TelecomDemoMsisdn.ShowcaseHealthy),
        new("PAY", "recharge", true, PlaywrightUiHelper.ShowroomEmail, "telecomSearchInput", "recharge",
            null, false, null, "Payments", TelecomDemoMsisdn.ShowcaseHealthy),
        new("SUS", "suspension", false, PlaywrightUiHelper.BackOfficeEmail, "hub-sus-suspension-type", "suspension",
            TelecomOperationKind.TemporarySuspension, true, "bo-sus-fraud", "Operations", TelecomDemoMsisdn.ShowcaseHealthy),
        new("CGT", "changeGsm", false, PlaywrightUiHelper.BackOfficeEmail, "hub-cgt-wizard", "changeGsm",
            TelecomOperationKind.ChangeGsmType, true, "bo-cgt-regulatory", "Operations", TelecomDemoMsisdn.ShowcaseHealthy),
        new("BDR", "badDebt", false, PlaywrightUiHelper.BackOfficeEmail, "hub-bdr-collection-action", "badDebt",
            TelecomOperationKind.BadDebtRecovery, true, "bo-bdr-writeoff", "Operations", TelecomDemoMsisdn.DebtSubscriber),
        new("TKO", "takeover", false, PlaywrightUiHelper.BackOfficeEmail, "hub-tko-wizard", "takeover",
            TelecomOperationKind.TakeOver, true, "bo-takeover", "Operations", TelecomDemoMsisdn.Hero),
        new("TRM", "termination", false, PlaywrightUiHelper.BackOfficeEmail, "hub-trm-termination-type", "termination",
            TelecomOperationKind.Termination, true, "bo-trm-regulatory", "Operations", TelecomDemoMsisdn.ShowcaseHealthy),
        new("RCN", "reconnect", false, PlaywrightUiHelper.SupervisorEmail, "hub-rcn-clearance-type", "reconnect",
            TelecomOperationKind.Reconnect, false, null, "Operations", TelecomDemoMsisdn.ShowcaseOperationalSuspended),
        new("RFD", "refund", false, PlaywrightUiHelper.SupervisorEmail, "hub-effective-mode", "refund",
            TelecomOperationKind.DepositRefundSettlement, true, "bo-rfd-syriatel", "Operations", TelecomDemoMsisdn.ShowcaseHealthy),
        new("DEV", "deviceSale", false, PlaywrightUiHelper.SupervisorEmail, "hub-dev-wizard", "deviceSale",
            TelecomOperationKind.DeviceSale, false, null, "Operations", TelecomDemoMsisdn.ShowcaseHealthy),
        new("VAS", "addpackage", false, PlaywrightUiHelper.SupervisorEmail, "hub-vas-wizard", "addpackage",
            TelecomOperationKind.ServiceModification, false, null, "Operations", TelecomDemoMsisdn.ShowcaseHealthy),
        new("SUP", "support", false, PlaywrightUiHelper.SupervisorEmail, "hub-support-issue-type", "support",
            null, false, null, "Tickets", TelecomDemoMsisdn.ShowcaseHealthy),
    ];

    public static MasterMatrixWizard GetWizard(string code) =>
        AllWizards.First(w => w.Code == code);

    public static async Task RunMasterMatrixJourneyAsync(
        MasterMatrixWizard wizard,
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        ICollection<string> tempFiles)
    {
        var submit = await RunPhase1PosHubAuthorizationAndExecuteAsync(
            wizard, fixture, page, baseUrl, tempFiles);

        await RunPhase2SingleSourceOfTruthAsync(wizard, fixture, page, baseUrl, submit);
        await RunPhase3Customer360TimelineAuditAsync(wizard, fixture, page, baseUrl, submit);
        await RunPhase4ExecutivePulseVerificationAsync(page, baseUrl);
    }

    // ── Phase 1 ──────────────────────────────────────────────────────────────

    public static async Task<MasterMatrixSubmitResult> RunPhase1PosHubAuthorizationAndExecuteAsync(
        MasterMatrixWizard wizard,
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.LoginViaUiAsync(
            page, baseUrl, wizard.HubPersonaEmail, PlaywrightUiHelper.DemoPassword);
        await PlaywrightUiHelper.GotoTelecomHubAsync(page, baseUrl);
        await AssertHubPbacMorphAsync(page, wizard);
        await LaunchHubWizardAsync(page, wizard);

        return wizard.Code switch
        {
            "ACT" => await SubmitActAsync(fixture, page, baseUrl, tempFiles),
            "MGR" => await SubmitMgrAsync(fixture, page, baseUrl, tempFiles),
            "SIM" => await SubmitSimAsync(fixture, page, baseUrl, wizard),
            "CNR" => await SubmitViaApiAsync(fixture, wizard, uploadDocument: true, confirmIfImmediate: false),
            "PAY" => await SubmitPayAsync(fixture, page, baseUrl, wizard),
            "SUS" => await SubmitViaApiAsync(fixture, wizard, uploadDocument: true, confirmIfImmediate: false),
            "CGT" => await SubmitCgtAsync(fixture, page, baseUrl, wizard, tempFiles),
            "BDR" => await SubmitViaApiAsync(fixture, wizard, uploadDocument: true, confirmIfImmediate: false),
            "TKO" => await SubmitViaApiAsync(fixture, wizard, uploadDocument: true, confirmIfImmediate: false),
            "TRM" => await SubmitViaApiAsync(fixture, wizard, uploadDocument: true, confirmIfImmediate: false),
            "RCN" => await SubmitRcnAsync(fixture, page, baseUrl, wizard, tempFiles),
            "RFD" => await SubmitViaApiAsync(fixture, wizard, uploadDocument: true, confirmIfImmediate: false),
            "DEV" => await SubmitDevAsync(fixture, page, baseUrl, wizard, tempFiles),
            "VAS" => await SubmitVasAsync(fixture, page, baseUrl, wizard),
            "SUP" => await SubmitSupAsync(fixture, page, baseUrl, wizard),
            _ => throw new NotSupportedException($"Wizard {wizard.Code} not mapped."),
        };
    }

    private static async Task AssertHubPbacMorphAsync(IPage page, MasterMatrixWizard wizard)
    {
        await Assertions.Expect(page.Locator(".telecom-hub-tiles-section")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(HubTileSelector(wizard))).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        var allowed = wizard.HubPersonaEmail switch
        {
            var e when e == PlaywrightUiHelper.ShowroomEmail => FrontlineTileCodes,
            var e when e == PlaywrightUiHelper.BackOfficeEmail => BackOfficeTileCodes,
            var e when e == PlaywrightUiHelper.SupervisorEmail => SupervisorTileCodes,
            _ => Array.Empty<string>(),
        };

        var denied = AllWizards
            .Where(w => !allowed.Contains(w.Code))
            .Select(HubTileSelector)
            .Distinct();

        foreach (var selector in denied)
        {
            await AssertAbsentFromDomAsync(page, selector);
        }
    }

    private static async Task LaunchHubWizardAsync(IPage page, MasterMatrixWizard wizard)
    {
        if (wizard.IsRechargeTile)
        {
            await Assertions.Expect(page.Locator("#telecomSearchInput")).ToBeVisibleAsync();
            return;
        }

        await page.Locator(HubTileSelector(wizard)).ClickAsync();
        await page.Locator("#telecomOpsWizardPanel").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        await Assertions.Expect(page.Locator($"[data-testid='{wizard.HubAnchorTestId}']")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
    }

    // ── Phase 2 ──────────────────────────────────────────────────────────────

    public static async Task RunPhase2SingleSourceOfTruthAsync(
        MasterMatrixWizard wizard,
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        MasterMatrixSubmitResult submit)
    {
        await PlaywrightUiHelper.LogoutViaUiAsync(page, baseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page, baseUrl, PlaywrightUiHelper.BackOfficeEmail, PlaywrightUiHelper.DemoPassword);

        await PlaywrightUiHelper.GotoTelecomHubAsync(page, baseUrl);
        await AssertAbsentFromDomAsync(page, ".telecom-table .bo-approve-op");
        await AssertAbsentFromDomAsync(page, ".telecom-table .bo-reject-op");

        await page.GotoAsync($"{baseUrl}/Telecom/BackOfficeDashboard", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await Assertions.Expect(page.Locator("#boTelecomQueuePanel")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        await page.Locator("#boTelecomQueueBody").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });

        if (!wizard.RequiresApproval || string.IsNullOrWhiteSpace(submit.OperationNumber))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(submit.OperationId))
        {
            var needsBo = await E2ETelecomOperationSeeds.RequiresBackOfficeApprovalAsync(
                fixture.AppFactory.Services, submit.OperationId);
            if (!needsBo)
            {
                return;
            }
        }

        var queueRow = page.Locator("#boTelecomQueueBody tr").Filter(new LocatorFilterOptions
        {
            HasText = submit.OperationNumber,
        });
        await queueRow.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });

        var approveBtn = queueRow.First.Locator(".bo-approve-op");
        await Assertions.Expect(approveBtn).ToBeVisibleAsync();
        await Assertions.Expect(queueRow.First.Locator(".bo-reject-op")).ToBeVisibleAsync();

        await approveBtn.ClickAsync();
        await page.WaitForTimeoutAsync(1_500);

        if (!string.IsNullOrWhiteSpace(submit.OperationId))
        {
            var status = await GetOperationStatusAsync(fixture.AppFactory.Services, submit.OperationId);
            Assert.NotEqual(TelecomOperationStatus.Failed, status);
        }
    }

    // ── Phase 3 ──────────────────────────────────────────────────────────────

    public static async Task RunPhase3Customer360TimelineAuditAsync(
        MasterMatrixWizard wizard,
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        MasterMatrixSubmitResult submit)
    {
        await PlaywrightUiHelper.LogoutViaUiAsync(page, baseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page, baseUrl, PlaywrightUiHelper.CallCenterEmail, PlaywrightUiHelper.DemoPassword);

        await PlaywrightUiHelper.GotoC360ProfileAsync(page, baseUrl, submit.CustomerId);
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Timeline");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = wizard.TimelineFilterLabel })
            .ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var marker = submit.TimelineMarker;
        var entry = page.Locator(".list-group-item").Filter(new LocatorFilterOptions { HasText = marker });
        await entry.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });

        var text = await entry.First.InnerTextAsync();
        Assert.Matches(new Regex(@"\d{4}|\d{1,2}[:/]|AM|PM|ص|م", RegexOptions.IgnoreCase), text);

        if (wizard.RequiresApproval
            && !string.IsNullOrWhiteSpace(submit.OperationNumber)
            && !string.IsNullOrWhiteSpace(submit.OperationId))
        {
            var needsBo = await E2ETelecomOperationSeeds.RequiresBackOfficeApprovalAsync(
                fixture.AppFactory.Services, submit.OperationId);
            if (needsBo)
            {
                var opsEntry = page.Locator(".list-group-item").Filter(new LocatorFilterOptions
                {
                    HasText = submit.OperationNumber,
                });
                if (await opsEntry.CountAsync() > 0)
                {
                    var opsText = await opsEntry.First.InnerTextAsync();
                    Assert.Matches(new Regex("OP-|Operation|عملية|Approved|BackOffice|موافقة", RegexOptions.IgnoreCase), opsText);
                }
            }
        }
    }

    // ── Phase 4 ──────────────────────────────────────────────────────────────

    public static async Task RunPhase4ExecutivePulseVerificationAsync(IPage page, string baseUrl)
    {
        await PlaywrightUiHelper.LogoutViaUiAsync(page, baseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page, baseUrl, PlaywrightUiHelper.ExecutiveMisEmail, PlaywrightUiHelper.DemoPassword);

        var navigationCount = 0;
        page.FrameNavigated += (_, _) => navigationCount++;

        await PlaywrightExecutiveCommandCenterHelper.GotoCommandCenterAsync(page, baseUrl);
        await Assertions.Expect(page.Locator("#commandCenterGate")).ToBeHiddenAsync();
        await PlaywrightExecutiveCommandCenterHelper.WaitForScorecardKpisAsync(page);

        var host = page.Locator("#executiveScorecardHost");
        var beforeText = await host.InnerTextAsync();

        var fromInput = page.Locator(".executive-scope-bar input[type='date']").First;
        var fromValue = await fromInput.InputValueAsync();
        if (!string.IsNullOrWhiteSpace(fromValue) && DateTime.TryParse(fromValue, out var fromDate))
        {
            await fromInput.FillAsync(fromDate.AddDays(-14).ToString("yyyy-MM-dd"));
        }

        var regionSelect = page.Locator(".executive-scope-bar select.form-select").First;
        if (await regionSelect.CountAsync() > 0)
        {
            var optionCount = await regionSelect.Locator("option").CountAsync();
            if (optionCount > 1)
            {
                await regionSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }
        }

        await PlaywrightExecutiveCommandCenterHelper.ApplyScopeAsync(page);
        await PlaywrightExecutiveCommandCenterHelper.WaitForScorecardKpisAsync(page);

        var afterText = await host.InnerTextAsync();
        Assert.True(navigationCount <= 1, "Executive scope apply should not trigger a full page reload.");
        Assert.False(string.IsNullOrWhiteSpace(beforeText));
        Assert.False(string.IsNullOrWhiteSpace(afterText));
        await Assertions.Expect(page.Locator(".executive-scope-bar")).ToBeVisibleAsync();
    }

    // ── Per-wizard submitters ────────────────────────────────────────────────

    private static async Task<MasterMatrixSubmitResult> SubmitActAsync(
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        ICollection<string> tempFiles)
    {
        var seed = await E2ETestDataHelper.ResolveActivationSeedAsync(fixture.AppFactory.Services);
        var customerSearch = await ResolveCustomerSearchTermAsync(fixture.AppFactory.Services, seed.CustomerId);
        var wizard = page.Locator("#telecomOpsWizardPanel");

        // ── Step A: customer + line type + package (slow, visible) ──
        var searchInput = wizard.Locator("input[type='search']").First;
        await PlaywrightDemoPresentationHelper.TypeSlowlyAsync(searchInput, customerSearch);
        await PlaywrightDemoPresentationHelper.ClickShowcaseAsync(
            page,
            wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Search" }).First);

        var firstResult = wizard.Locator(".list-group-item-action").First;
        await firstResult.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        await PlaywrightDemoPresentationHelper.ClickRoutineAsync(page, firstResult);
        await page.Locator("#wizActivationLineType").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var productsTask = page.WaitForResponseAsync(
            r => r.Url.Contains("/Product/GetMigrationEligibleProducts", StringComparison.OrdinalIgnoreCase) && r.Ok,
            new PageWaitForResponseOptions { Timeout = 90_000 });

        await PlaywrightDemoPresentationHelper.SelectOptionRoutineAsync(
            page,
            page.Locator("#wizActivationLineType"),
            new SelectOptionValue { Index = 1 });

        try
        {
            await productsTask;
        }
        catch (TimeoutException)
        {
            // Products may already be cached from a prior hub action in the same session.
        }

        // ── Step B: MSISDN reserve (must not clear primary-subscriber alert) ──
        await ReserveActivationMsisdnAsync(page, wizard, seed.Msisdn);

        await SelectActivationOfferingAsync(page, wizard);

        // ── Step C: ICCID compliance demo (step 1 complete — Vue simIccid, not DOM only) ──
        if (PlaywrightDemoPresentationHelper.IsPresentationMode())
        {
            await DemonstrateActIccidValidationAsync(page, wizard, seed);
        }

        // ── Step D: registration step → KYC → draft → CBS confirm ──
        await AdvanceHubWizardToRegistrationStepAsync(page, wizard);

        var kycPath = CreateTempPdf(tempFiles);
        var kycDropzone = wizard.Locator(".kyc-dropzone");
        await kycDropzone.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var kycUploadTask = page.WaitForResponseAsync(
            r => r.Url.Contains("/Telecom/UploadKycDocument", StringComparison.OrdinalIgnoreCase) && r.Ok,
            new PageWaitForResponseOptions { Timeout = 120_000 });
        await wizard.Locator(".kyc-dropzone input[type='file']").SetInputFilesAsync(kycPath);
        try
        {
            await kycUploadTask;
        }
        catch (TimeoutException)
        {
            // Upload may complete via cached session; fall through to locked-state wait.
        }

        await wizard.Locator(".kyc-dropzone--locked").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });
        await page.WaitForFunctionAsync(
            "() => !!(window.__telecomHubE2E?.getWizard()?.kycDocumentReferenceId)",
            new PageWaitForFunctionOptions { Timeout = 60_000 });

        var saveBtn = wizard.Locator(".telecom-wizard-step button.btn-primary:not([disabled])").Filter(
            new LocatorFilterOptions
            {
                HasTextRegex = new Regex("Save request|Create draft|draft|حفظ", RegexOptions.IgnoreCase),
            });
        await Assertions.Expect(saveBtn.First).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions
        {
            Timeout = 60_000,
        });

        var createOpTask = page.WaitForResponseAsync(
            r => r.Url.Contains("/Telecom/CreateTelecomOperation", StringComparison.OrdinalIgnoreCase),
            new PageWaitForResponseOptions { Timeout = 120_000 });
        await PlaywrightDemoPresentationHelper.ClickShowcaseAsync(page, saveBtn.First);
        IResponse? createResponse = null;
        try
        {
            createResponse = await createOpTask;
        }
        catch (TimeoutException)
        {
            var swal = page.Locator(".swal2-container");
            if (await swal.CountAsync() > 0)
            {
                var title = await swal.Locator(".swal2-title").InnerTextAsync();
                var body = await swal.Locator("#swal2-html-container, .swal2-html-container").InnerTextAsync();
                throw new InvalidOperationException(
                    $"Create draft blocked — SweetAlert: {title.Trim()} / {body.Trim()}");
            }

            throw new InvalidOperationException("CreateTelecomOperation API did not respond after Save request.");
        }

        if (createResponse is not null && !createResponse.Ok)
        {
            throw new InvalidOperationException(
                $"CreateTelecomOperation failed with HTTP {(int)createResponse.Status} {createResponse.StatusText}.");
        }

        if (createResponse is not null)
        {
            await AssertCreateTelecomOperationEnvelopeAsync(createResponse);
        }

        var opNumber = await WaitForHubWizardOpNumberAsync(page, wizard);

        await RecordActivationDepositIfRequiredAsync(page, wizard);

        var confirmBtn = wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Confirm CBS|HLR|تأكيد", RegexOptions.IgnoreCase),
        });
        await Assertions.Expect(confirmBtn.First).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions
        {
            Timeout = 60_000,
        });

        var confirmTask = page.WaitForResponseAsync(
            r => r.Url.Contains("/Telecom/ConfirmTelecomOperation", StringComparison.OrdinalIgnoreCase),
            new PageWaitForResponseOptions { Timeout = 180_000 });
        await PlaywrightDemoPresentationHelper.ClickShowcaseAsync(page, confirmBtn.First);
        try
        {
            await confirmTask;
        }
        catch (TimeoutException)
        {
            var swal = page.Locator(".swal2-container");
            if (await swal.CountAsync() > 0)
            {
                var title = await swal.Locator(".swal2-title").InnerTextAsync();
                var body = await swal.Locator("#swal2-html-container, .swal2-html-container").InnerTextAsync();
                throw new InvalidOperationException(
                    $"CBS confirm blocked — SweetAlert: {title.Trim()} / {body.Trim()}");
            }

            throw;
        }

        await page.WaitForFunctionAsync(
            "() => !!window.__telecomHubE2E?.getWizard()?.confirmed",
            new PageWaitForFunctionOptions { Timeout = 180_000 });
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            fixture.AppFactory.Services, seed.SubscriberProfileId, TelecomOperationKind.NewActivation);
        Assert.NotNull(op);

        return new MasterMatrixSubmitResult(
            seed.CustomerId,
            seed.SubscriberProfileId,
            op!.Id,
            opNumber ?? op.Number,
            null,
            null,
            opNumber ?? op.Number);
    }

    private static async Task SelectActivationOfferingAsync(IPage page, ILocator wizard)
    {
        var offeringSelect = page.Locator("#wizTargetOffer");
        await offeringSelect.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
        await Assertions.Expect(offeringSelect.Locator("option"))
            .Not.ToHaveCountAsync(1, new LocatorAssertionsToHaveCountOptions { Timeout = 90_000 });

        var offeringOption = offeringSelect.Locator("option").Filter(new LocatorFilterOptions { HasText = "YA_HALA_30" });
        string offeringValue;
        if (await offeringOption.CountAsync() > 0)
        {
            offeringValue = await offeringOption.First.GetAttributeAsync("value")
                ?? throw new InvalidOperationException("YA_HALA_30 offering option not found.");
        }
        else
        {
            offeringValue = await offeringSelect.Locator("option").Nth(1).GetAttributeAsync("value")
                ?? throw new InvalidOperationException("No activation offering options loaded.");
        }

        await PlaywrightDemoPresentationHelper.SelectOptionRoutineAsync(
            page,
            offeringSelect,
            new SelectOptionValue { Value = offeringValue });
        await Assertions.Expect(offeringSelect).ToHaveValueAsync(offeringValue, new LocatorAssertionsToHaveValueOptions
        {
            Timeout = 30_000,
        });
        await page.EvaluateAsync(
            """
            (productId) => {
                const bridge = window.__telecomHubE2E;
                if (!bridge?.getWizard) return false;
                bridge.getWizard().migrationTargetProductId = productId;
                return true;
            }
            """,
            offeringValue);
    }

    private static async Task AdvanceHubWizardToRegistrationStepAsync(IPage page, ILocator wizard)
    {
        await page.WaitForFunctionAsync(
            "() => typeof window.Swal !== 'undefined' && typeof window.Swal.fire === 'function'",
            new PageWaitForFunctionOptions { Timeout = 30_000 });

        var nextReg = wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|Registration|التالي", RegexOptions.IgnoreCase),
        }).First;

        if (PlaywrightDemoPresentationHelper.IsPresentationMode())
        {
            await PlaywrightDemoPresentationHelper.ClickShowcaseAsync(page, nextReg);
        }
        else
        {
            await nextReg.ClickAsync();
        }

        var kycDropzone = wizard.Locator("div.telecom-wizard-step .kyc-dropzone");
        try
        {
            await kycDropzone.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30_000,
            });
        }
        catch (TimeoutException ex)
        {
            var swal = page.Locator(".swal2-container");
            if (await swal.CountAsync() > 0)
            {
                var title = await swal.Locator(".swal2-title").InnerTextAsync();
                var body = await swal.Locator("#swal2-html-container, .swal2-html-container").InnerTextAsync();
                throw new InvalidOperationException(
                    $"Hub wizard blocked step 2 — SweetAlert: {title.Trim()} / {body.Trim()}");
            }

            var wizardStep = await page.EvaluateAsync<int?>(
                """
                () => {
                    let comp = document.querySelector('#telecomOpsWizardPanel')?.__vueParentComponent;
                    while (comp) {
                        const w = comp.ctx?.wizard?.value ?? comp.ctx?.wizard;
                        if (w && typeof w === 'object') return w.step ?? null;
                        comp = comp.parent;
                    }
                    return null;
                }
                """);
            throw new InvalidOperationException(
                $"Hub wizard did not reach KYC step (wizard.step={wizardStep?.ToString() ?? "unknown"}).",
                ex);
        }
    }

    private static async Task RecordActivationDepositIfRequiredAsync(IPage page, ILocator wizard)
    {
        var fetchCashier = wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Fetch from cashier|الصندوق", RegexOptions.IgnoreCase),
        });
        if (await fetchCashier.CountAsync() == 0 || !await fetchCashier.First.IsVisibleAsync())
        {
            return;
        }

        var paymentRef = wizard.Locator("input.font-monospace").Filter(new LocatorFilterOptions
        {
            Has = page.Locator("[placeholder*='Reference'], [placeholder*='مرجع']"),
        });
        if (await paymentRef.CountAsync() == 0)
        {
            paymentRef = wizard.Locator("input.font-monospace").Last;
        }

        await paymentRef.FillAsync("REC-2026-ACT");

        var fetchTask = page.WaitForResponseAsync(
            r => r.Url.Contains("/Telecom/FetchCashierPayment", StringComparison.OrdinalIgnoreCase) && r.Ok,
            new PageWaitForResponseOptions { Timeout = 60_000 });
        await fetchCashier.First.ClickAsync();
        try
        {
            await fetchTask;
        }
        catch (TimeoutException)
        {
            // Fall through — bridge wait below surfaces cashier errors.
        }

        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);
        await page.WaitForFunctionAsync(
            "() => !!window.__telecomHubE2E?.getWizard()?.paymentCashierLocked",
            new PageWaitForFunctionOptions { Timeout = 60_000 });

        var recordPayment = wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Record payment|تسجيل الدفع", RegexOptions.IgnoreCase),
        });
        await Assertions.Expect(recordPayment.First).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions
        {
            Timeout = 60_000,
        });
        var recordPaymentTask = page.WaitForResponseAsync(
            r => r.Url.Contains("/Telecom/RecordSellingLinePayment", StringComparison.OrdinalIgnoreCase) && r.Ok,
            new PageWaitForResponseOptions { Timeout = 60_000 });
        await recordPayment.First.ClickAsync();
        try
        {
            await recordPaymentTask;
        }
        catch (TimeoutException)
        {
            // Fall through — bridge wait below surfaces payment errors.
        }

        await page.WaitForFunctionAsync(
            "() => !!window.__telecomHubE2E?.getWizard()?.paymentRecorded",
            new PageWaitForFunctionOptions { Timeout = 60_000 });
    }

    private static async Task EnsureActWizardIccidAsync(IPage page, string msisdn)
    {
        var iccidInput = page.Locator("#wizActivateIccid");
        var current = (await iccidInput.InputValueAsync()).Trim();
        if (current.Length >= 19)
        {
            return;
        }

        var derived = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(msisdn);
        if (!string.IsNullOrWhiteSpace(derived) && derived.Length >= 19)
        {
            if (!await SetActWizardSimIccidAsync(page, derived))
            {
                throw new InvalidOperationException(
                    $"Could not set derived ICCID on Hub wizard (msisdn={msisdn}, iccid={derived}).");
            }

            await Assertions.Expect(iccidInput).ToHaveValueAsync(
                new Regex(@".{19,}"),
                new LocatorAssertionsToHaveValueOptions { Timeout = 10_000 });
            return;
        }

        current = (await iccidInput.InputValueAsync()).Trim();
        Assert.True(
            current.Length >= 19,
            $"ICCID must be populated after MSISDN reserve (got '{current}', msisdn={msisdn}).");
    }

    private static Task<bool> SetActWizardSimIccidAsync(IPage page, string iccid) =>
        page.EvaluateAsync<bool>(
            """
            (value) => {
                const bridge = window.__telecomHubE2E;
                if (bridge?.getWizard) {
                    bridge.getWizard().simIccid = value;
                    return true;
                }
                return false;
            }
            """,
            iccid);

    private static Task<bool> ClearActWizardSimIccidAsync(IPage page) =>
        SetActWizardSimIccidAsync(page, string.Empty);

    /// <summary>
    /// Compliance beat: empty ICCID → SweetAlert → restore ICCID without reopening the picker.
    /// </summary>
    private static async Task DemonstrateActIccidValidationAsync(IPage page, ILocator wizard, ActivationSeedBundle seed)
    {
        Assert.True(
            await ClearActWizardSimIccidAsync(page),
            "Could not clear Vue wizard.simIccid for ICCID compliance demo.");

        var nextReg = wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|Registration|التالي", RegexOptions.IgnoreCase),
        });
        await PlaywrightDemoPresentationHelper.ClickShowcaseAsync(page, nextReg.First);

        await PlaywrightDemoPresentationHelper.AssertSwalAsync(
            page,
            "Missing details",
            "19-digit SIM ICCID");
        await PlaywrightDemoPresentationHelper.DismissSwalOkAsync(page);

        var iccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(seed.Msisdn);
        if (string.IsNullOrWhiteSpace(iccid) || iccid.Length < 19)
        {
            await ReserveActivationMsisdnAsync(page, wizard, seed.Msisdn, showcaseReserve: false);
        }
        else if (!await SetActWizardSimIccidAsync(page, iccid))
        {
            await ReserveActivationMsisdnAsync(page, wizard, seed.Msisdn, showcaseReserve: false);
            await EnsureActWizardIccidAsync(page, seed.Msisdn);
        }

        var iccidAfter = (await page.Locator("#wizActivateIccid").InputValueAsync()).Trim();
        Assert.True(
            iccidAfter.Length >= 19,
            $"ICCID must be populated after compliance demo (got '{iccidAfter}').");
    }

    private static async Task ReserveActivationMsisdnAsync(
        IPage page,
        ILocator wizard,
        string preferredMsisdn,
        bool showcaseReserve = false)
    {
        PlaywrightDemoPresentationHelper.EnsurePageOpen(page);

        // Only clear the reserved-MSISDN chip — not the primary-subscriber alert-success above it.
        var clearMsisdn = wizard.Locator(".alert-success:has(strong.font-monospace) .btn-link");
        if (await clearMsisdn.CountAsync() > 0)
        {
            await PlaywrightDemoPresentationHelper.ClickRoutineAsync(page, clearMsisdn.First);
        }

        var msisdnBrowse = wizard.Locator("button[type='button']:has(i.bi-search)");
        await msisdnBrowse.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        await Assertions.Expect(msisdnBrowse.First).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions
        {
            Timeout = 30_000,
        });

        var poolTask = page.WaitForResponseAsync(
            r => r.Url.Contains("/Telecom/GetMsisdnAssetPoolList", StringComparison.OrdinalIgnoreCase) && r.Ok,
            new PageWaitForResponseOptions { Timeout = 90_000 });

        if (showcaseReserve)
        {
            await PlaywrightDemoPresentationHelper.ClickShowcaseAsync(page, msisdnBrowse.First);
        }
        else
        {
            await PlaywrightDemoPresentationHelper.ClickRoutineAsync(page, msisdnBrowse.First);
        }

        try
        {
            await poolTask;
        }
        catch (TimeoutException)
        {
            // Pool may already be cached in-session.
        }

        var msisdnModal = page.Locator(".activate-msisdn-picker-host");
        await msisdnModal.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        await msisdnModal.Locator(".msisdn-picker-row").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });

        var msisdnRow = msisdnModal.Locator(".msisdn-picker-row").Filter(new LocatorFilterOptions
        {
            HasText = preferredMsisdn,
        });
        if (await msisdnRow.CountAsync() == 0)
        {
            msisdnRow = msisdnModal.Locator(".msisdn-picker-row").First;
        }

        var reservedMsisdn = (await msisdnRow.Locator("td").First.InnerTextAsync()).Trim();

        var reservedViaApp = await page.EvaluateAsync<bool>(
            """
            async (preferredMsisdn) => {
                const bridge = window.__telecomHubE2E;
                if (!bridge?.selectWizardActivateMsisdn || !bridge.getWizard) return false;
                const rows = bridge.getWizard().activateMsisdnPoolRows || [];
                const row = rows.find((r) => String(r?.msisdn ?? r?.Msisdn ?? '').includes(preferredMsisdn)) || rows[0];
                if (!row) return false;
                await bridge.selectWizardActivateMsisdn(row);
                return true;
            }
            """,
            preferredMsisdn);

        if (!reservedViaApp)
        {
            var reserveBtn = msisdnRow.Locator("button.btn-danger");
            if (showcaseReserve)
            {
                await PlaywrightDemoPresentationHelper.ClickShowcaseAsync(page, reserveBtn);
            }
            else
            {
                await PlaywrightDemoPresentationHelper.ClickRoutineAsync(page, reserveBtn);
            }
        }

        await msisdnModal.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 60_000,
        });

        await Assertions.Expect(wizard.Locator(".alert-success strong.font-monospace"))
            .ToContainTextAsync(reservedMsisdn, new LocatorAssertionsToContainTextOptions { Timeout = 60_000 });

        await EnsureActWizardIccidAsync(page, reservedMsisdn);
    }

    private static async Task<MasterMatrixSubmitResult> SubmitMgrAsync(
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        ICollection<string> tempFiles)
    {
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(fixture.AppFactory.Services, TelecomDemoMsisdn.ShowcaseHealthy);
        await PlaywrightUiHelper.GotoC360WizardAsync(page, baseUrl, line.CustomerId, "migrate", line.LineKey);

        var offeringSelect = page.Locator("[data-testid='c360-mgr-offering-select']");
        await offeringSelect.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var targetOption = offeringSelect.Locator("option").Filter(new LocatorFilterOptions { HasText = "YA_HALA_PLUS" });
        if (await targetOption.CountAsync() == 0)
        {
            targetOption = offeringSelect.Locator("option").Nth(1);
        }

        var offeringValue = await targetOption.First.GetAttributeAsync("value")
            ?? throw new InvalidOperationException("Migration offering not found.");
        await offeringSelect.SelectOptionAsync(offeringValue);
        await page.Locator("[data-testid='c360-effective-mode']").SelectOptionAsync("scheduled");

        var wizard = page.Locator("#c360ProvWizardModal");
        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        await PlaywrightBssWizardHelper.CreateC360WizardDraftAsync(page);
        var opNumber = await WaitForOpNumberInWizardAsync(wizard);

        var kycPath = CreateTempPdf(tempFiles);
        var fileInput = wizard.Locator("input[type='file']").First;
        if (await fileInput.CountAsync() > 0)
        {
            await fileInput.SetInputFilesAsync(kycPath);
            await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
            {
                NameRegex = new Regex("Mark document|Document|وثيقة", RegexOptions.IgnoreCase),
            }).ClickAsync();
        }

        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Confirm CBS|HLR|تأكيد", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await wizard.Locator("text=/Confirmed|success|provision|تم/i").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });
        await PlaywrightUiHelper.CloseC360WizardAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            fixture.AppFactory.Services, line.SubscriberProfileId, TelecomOperationKind.Migration);
        Assert.NotNull(op);

        return new MasterMatrixSubmitResult(
            line.CustomerId,
            line.SubscriberProfileId,
            op!.Id,
            opNumber ?? op.Number,
            null,
            null,
            opNumber ?? op.Number);
    }

    private static async Task<MasterMatrixSubmitResult> SubmitSimAsync(
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        MasterMatrixWizard wizard)
    {
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(fixture.AppFactory.Services, wizard.DemoMsisdn);
        await PlaywrightUiHelper.GotoC360WizardAsync(page, baseUrl, line.CustomerId, "simswap", line.LineKey);

        await page.Locator("[data-testid='c360-sim-replacement-reason']").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        await PlaywrightBssWizardHelper.CreateC360WizardDraftAsync(page);

        var modal = page.Locator("#c360ProvWizardModal");
        var opNumber = await WaitForOpNumberInWizardAsync(modal);

        await modal.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Confirm CBS|HLR|تأكيد", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await modal.Locator("text=/Confirmed|success|provision|تم/i").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });
        await PlaywrightUiHelper.CloseC360WizardAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            fixture.AppFactory.Services, line.SubscriberProfileId, TelecomOperationKind.SimSwap);
        Assert.NotNull(op);

        return new MasterMatrixSubmitResult(
            line.CustomerId,
            line.SubscriberProfileId,
            op!.Id,
            opNumber ?? op.Number,
            null,
            null,
            opNumber ?? op.Number);
    }

    private static async Task<MasterMatrixSubmitResult> SubmitPayAsync(
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        MasterMatrixWizard wizard)
    {
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(fixture.AppFactory.Services, wizard.DemoMsisdn);
        await page.Locator("#telecomSearchInput").FillAsync(line.Msisdn);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = new Regex("Recharge|شحن", RegexOptions.IgnoreCase),
        }).First.ClickAsync();

        var gatewayRef = $"E2E-MTX-{Guid.NewGuid():N}"[..20];
        await PlaywrightUiHelper.CompleteWalletRechargeSwalAsync(page, RechargeAmount, gatewayRef);
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        var ledger = await E2ETestDataHelper.GetLatestCompletedRechargeByGatewayRefAsync(
            fixture.AppFactory.Services, line.CustomerId, gatewayRef);
        Assert.NotNull(ledger);

        return new MasterMatrixSubmitResult(
            line.CustomerId,
            line.SubscriberProfileId,
            null,
            null,
            gatewayRef,
            null,
            ledger!.PaymentNumber);
    }

    private static async Task<MasterMatrixSubmitResult> SubmitCgtAsync(
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        MasterMatrixWizard wizard,
        ICollection<string> tempFiles)
    {
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(fixture.AppFactory.Services, wizard.DemoMsisdn);
        await PlaywrightUiHelper.GotoC360WizardAsync(page, baseUrl, line.CustomerId, "changeGsm", line.LineKey);

        await page.Locator("[data-testid='c360-cgt-migration-path']").SelectOptionAsync("Regulatory");
        await page.Locator("[data-testid='c360-cgt-regulatory-doc-no']").FillAsync($"REG-MTX-{Guid.NewGuid():N}"[..12]);
        await page.Locator("[data-testid='c360-cgt-regulatory-upload']").SetInputFilesAsync(CreateTempPdf(tempFiles));
        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        await PlaywrightBssWizardHelper.CreateC360WizardDraftAsync(page);

        var modal = page.Locator("#c360ProvWizardModal");
        var opNumber = await WaitForOpNumberInWizardAsync(modal);
        await PlaywrightUiHelper.CloseC360WizardAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            fixture.AppFactory.Services, line.SubscriberProfileId, TelecomOperationKind.ChangeGsmType);
        Assert.NotNull(op);

        return new MasterMatrixSubmitResult(
            line.CustomerId,
            line.SubscriberProfileId,
            op!.Id,
            opNumber ?? op.Number,
            null,
            null,
            opNumber ?? op.Number);
    }

    private static async Task<MasterMatrixSubmitResult> SubmitRcnAsync(
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        MasterMatrixWizard wizard,
        ICollection<string> tempFiles)
    {
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(fixture.AppFactory.Services, wizard.DemoMsisdn);
        await PlaywrightUiHelper.GotoC360WizardAsync(page, baseUrl, line.CustomerId, "reconnect", line.LineKey);

        await page.Locator("[data-testid='c360-rcn-clearance-type']").SelectOptionAsync("Operational");
        await page.Locator("#c360ProvWizardModal input[type='text']").First.FillAsync("E2E master matrix operational reconnect");
        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        await PlaywrightBssWizardHelper.CreateC360WizardDraftAsync(page);

        var modal = page.Locator("#c360ProvWizardModal");
        var opNumber = await WaitForOpNumberInWizardAsync(modal);

        await modal.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Confirm CBS|HLR|تأكيد", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await modal.Locator("text=/Confirmed|success|provision|تم/i").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });
        await PlaywrightUiHelper.CloseC360WizardAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            fixture.AppFactory.Services, line.SubscriberProfileId, TelecomOperationKind.Reconnect);
        Assert.NotNull(op);

        return new MasterMatrixSubmitResult(
            line.CustomerId,
            line.SubscriberProfileId,
            op!.Id,
            opNumber ?? op.Number,
            null,
            null,
            opNumber ?? op.Number);
    }

    private static async Task<MasterMatrixSubmitResult> SubmitDevAsync(
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        MasterMatrixWizard wizard,
        ICollection<string> tempFiles)
    {
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(fixture.AppFactory.Services, wizard.DemoMsisdn);
        await PlaywrightUiHelper.GotoC360WizardAsync(page, baseUrl, line.CustomerId, "deviceSale", line.LineKey);

        await page.Locator("[data-testid='c360-dev-sale-type']").SelectOptionAsync("Installment");
        var inventorySelect = page.Locator("[data-testid='c360-dev-inventory-select']");
        await inventorySelect.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        if (await inventorySelect.Locator("option").CountAsync() > 1)
        {
            await inventorySelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        await PlaywrightBssWizardHelper.ClickC360WizardNextAsync(page);
        await PlaywrightBssWizardHelper.CreateC360WizardDraftAsync(page);
        var modal = page.Locator("#c360ProvWizardModal");
        var opNumber = await WaitForOpNumberInWizardAsync(modal);

        await modal.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Confirm CBS|HLR|تأكيد", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await modal.Locator("text=/Confirmed|success|provision|تم/i").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });
        await PlaywrightUiHelper.CloseC360WizardAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            fixture.AppFactory.Services, line.SubscriberProfileId, TelecomOperationKind.DeviceSale);
        Assert.NotNull(op);

        return new MasterMatrixSubmitResult(
            line.CustomerId,
            line.SubscriberProfileId,
            op!.Id,
            opNumber ?? op.Number,
            null,
            null,
            opNumber ?? op.Number);
    }

    private static async Task<MasterMatrixSubmitResult> SubmitVasAsync(
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        MasterMatrixWizard wizard)
    {
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(fixture.AppFactory.Services, wizard.DemoMsisdn);
        await PlaywrightUiHelper.GotoC360WizardAsync(page, baseUrl, line.CustomerId, "addpackage", line.LineKey);

        await page.Locator("[data-testid='c360-vas-wizard']").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
        });
        await SelectVasServiceAsync(page, VasServiceCode);
        await page.Locator("[data-testid='c360-vas-action-select']").SelectOptionAsync("Activate");

        var modal = page.Locator("#c360ProvWizardModal");
        await modal.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|VAS|Activate|التالي", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await page.Locator(".swal2-success").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            fixture.AppFactory.Services, line.SubscriberProfileId, TelecomOperationKind.ServiceModification);
        Assert.NotNull(op);

        return new MasterMatrixSubmitResult(
            line.CustomerId,
            line.SubscriberProfileId,
            op!.Id,
            op.Number,
            null,
            null,
            op.Number);
    }

    private static async Task<MasterMatrixSubmitResult> SubmitSupAsync(
        TelecomE2EFixture fixture,
        IPage page,
        string baseUrl,
        MasterMatrixWizard wizard)
    {
        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(fixture.AppFactory.Services, wizard.DemoMsisdn);
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, baseUrl, line.CustomerId);

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = new Regex("Support ticket|تذكرة", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await page.Locator("[data-testid='c360-support-wizard']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        await page.Locator("[data-testid='c360-support-issue-type']").SelectOptionAsync("0");
        await page.Locator("[data-testid='c360-support-wizard'] textarea")
            .FillAsync("Master matrix E2E — network quality degradation in sector.");

        var modal = page.Locator("#c360ProvWizardModal");
        await modal.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Create|إنشاء", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await modal.Locator("strong").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        var ticketNumber = (await modal.Locator("strong").First.InnerTextAsync()).Trim();
        Assert.Matches(new Regex("TT-|TKT-", RegexOptions.IgnoreCase), ticketNumber);
        await PlaywrightUiHelper.CloseC360WizardAsync(page);

        return new MasterMatrixSubmitResult(
            line.CustomerId,
            line.SubscriberProfileId,
            null,
            null,
            null,
            ticketNumber,
            ticketNumber);
    }

    private static async Task<MasterMatrixSubmitResult> SubmitViaApiAsync(
        TelecomE2EFixture fixture,
        MasterMatrixWizard wizard,
        bool uploadDocument,
        bool confirmIfImmediate)
    {
        var client = CreateApiClient(fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client));

        var kind = wizard.OperationKind
            ?? throw new InvalidOperationException($"Wizard {wizard.Code} has no operation kind.");
        var scenarioKey = wizard.ApprovalScenarioKey ?? $"master-matrix-{wizard.Code.ToLowerInvariant()}";

        var payload = wizard.RequiresApproval
            ? await E2ETelecomOperationSeeds.BuildBackOfficeScenarioPayloadAsync(
                fixture.AppFactory.Services, kind, scenarioKey)
            : E2ETelecomOperationSeeds.BuildCreatePayload(
                await E2ETelecomOperationSeeds.ResolveAsync(fixture.AppFactory.Services, kind, scenarioKey));

        var operationId = await client.CreateOperationAsync(payload);
        if (uploadDocument)
        {
            await client.UploadDocumentAsync(operationId);
        }

        if (confirmIfImmediate && !wizard.RequiresApproval)
        {
            await client.ConfirmAsync(operationId);
        }

        var snap = await GetOperationSnapshotByIdAsync(fixture.AppFactory.Services, operationId);
        Assert.NotNull(snap);

        return new MasterMatrixSubmitResult(
            await E2ETestDataHelper.ResolveDemoCustomerIdForMsisdnAsync(fixture.AppFactory.Services, wizard.DemoMsisdn),
            snap!.SubscriberProfileId,
            snap.Id,
            snap.Number,
            null,
            null,
            snap.Number);
    }

    // ── Utilities ───────────────────────────────────────────────────────────

    private static string HubTileSelector(MasterMatrixWizard wizard) =>
        wizard.IsRechargeTile
            ? ".telecom-hub-tiles-section .tile-recharge"
            : $"[data-testid='hub-wizard-tile-{wizard.HubTileKind}']";

    private static async Task AssertAbsentFromDomAsync(IPage page, string selector)
    {
        var count = await page.Locator(selector).CountAsync();
        Assert.Equal(0, count);
    }

    private static async Task AssertCreateTelecomOperationEnvelopeAsync(IResponse response)
    {
        var json = await response.JsonAsync();
        if (json is null)
        {
            return;
        }

        if (json.Value.TryGetProperty("code", out var codeProp) && codeProp.GetInt32() != 200)
        {
            var message = json.Value.TryGetProperty("message", out var messageProp)
                ? messageProp.GetString()
                : null;
            throw new InvalidOperationException(
                $"CreateTelecomOperation rejected (code {codeProp.GetInt32()}): {message ?? "unknown"}");
        }
    }

    private static async Task<string> WaitForHubWizardOpNumberAsync(IPage page, ILocator wizard)
    {
        try
        {
            await page.WaitForFunctionAsync(
                "() => !!(window.__telecomHubE2E?.getWizard()?.createdOperationNumber)",
                new PageWaitForFunctionOptions { Timeout = 90_000 });
        }
        catch (TimeoutException ex)
        {
            var swal = page.Locator(".swal2-container");
            if (await swal.CountAsync() > 0)
            {
                var title = await swal.Locator(".swal2-title").InnerTextAsync();
                var body = await swal.Locator("#swal2-html-container, .swal2-html-container").InnerTextAsync();
                throw new InvalidOperationException(
                    $"Draft not created — SweetAlert: {title.Trim()} / {body.Trim()}",
                    ex);
            }

            throw;
        }

        var opNumber = await page.EvaluateAsync<string>(
            "() => (window.__telecomHubE2E?.getWizard()?.createdOperationNumber || '').trim()");
        if (!string.IsNullOrWhiteSpace(opNumber))
        {
            await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);
            return opNumber;
        }

        return await WaitForOpNumberInWizardAsync(wizard);
    }

    private static async Task<string> WaitForOpNumberInWizardAsync(ILocator wizard)
    {
        var opLocator = wizard.Locator("text=/OP-/").First;
        await opLocator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        var text = (await opLocator.InnerTextAsync()).Trim();
        var match = Regex.Match(text, @"OP-[\w-]+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : text;
    }

    private static async Task SelectVasServiceAsync(IPage page, string serviceCode)
    {
        var select = page.Locator("[data-testid='c360-vas-service-select']");
        if (await select.CountAsync() == 0)
        {
            select = page.Locator("#c360ProvWizardModal select").First;
        }

        var options = await select.Locator("option").AllInnerTextsAsync();
        for (var i = 0; i < options.Count; i++)
        {
            if (options[i].Contains(serviceCode, StringComparison.OrdinalIgnoreCase))
            {
                await select.SelectOptionAsync(new SelectOptionValue { Index = i });
                return;
            }
        }

        if (await select.Locator("option").CountAsync() > 1)
        {
            await select.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }
    }

    private static TelecomE2EClient CreateApiClient(TelecomE2EFixture fixture)
    {
        var http = fixture.AppFactory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        return new TelecomE2EClient(http, fixture.SimulatorClient);
    }

    private static async Task<TelecomOperationSnapshot?> GetOperationSnapshotByIdAsync(
        IServiceProvider services,
        string operationId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Id == operationId)
            .Select(o => new TelecomOperationSnapshot(
                o.Id,
                o.Number,
                o.Kind,
                o.Status,
                o.SubscriberProfileId))
            .FirstOrDefaultAsync();
    }

    private static async Task<TelecomOperationStatus> GetOperationStatusAsync(
        IServiceProvider services,
        string operationId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => o.Id == operationId)
            .Select(o => o.Status)
            .FirstAsync();
    }

    private static async Task<string> ResolveCustomerSearchTermAsync(
        IServiceProvider services,
        string customerId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        var nationalId = await db.Set<IndividualCustomer>().AsNoTracking()
            .Where(c => !c.IsDeleted && c.Id == customerId)
            .Select(c => c.NationalId)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(nationalId))
        {
            return nationalId.Trim();
        }

        var accountNumber = await db.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Id == customerId)
            .Select(c => c.AccountNumber)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(accountNumber))
        {
            return accountNumber.Trim();
        }

        return customerId;
    }

    private static string CreateTempPdf(ICollection<string> registry)
    {
        var path = Path.Combine(Path.GetTempPath(), $"master-matrix-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, "%PDF-1.4 Master Matrix E2E Placeholder", Encoding.UTF8);
        registry.Add(path);
        return path;
    }
}
