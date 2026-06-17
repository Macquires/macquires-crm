using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Application.Common.Telecom;
using ASPNET.E2E.Tests.Infrastructure;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.UI.Telecom;

/// <summary>
/// Stateful Customer 360 master suite — UI + ledger + wizards + ticket lifecycle verification.
/// For Hub + List + full matrix see <see cref="TelecomBssFullSystemMasterTests"/>.
/// Run: dotnet test --filter "FullyQualifiedName~TelecomCustomer360ComprehensiveMasterTests|TelecomBssFullSystemMasterTests"
/// </summary>
[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "UI")]
[Trait("Category", "Comprehensive")]
public sealed class TelecomCustomer360ComprehensiveMasterTests
{
    private const decimal RechargeAmount = 5_000m;
    private const string VasServiceCode = "VAS_CALLER_ID";

    private readonly TelecomE2EFixture _fixture;

    public TelecomCustomer360ComprehensiveMasterTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task MasterSuite_AllCases_StatefulVerification()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        var line = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseHealthy);
        var notProvisionedLine = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseNotProvisioned);
        var suspendedLine = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.ShowcaseOperationalSuspended);

        var subscriptionId = await E2ETestDataHelper.ResolveSubscriptionIdForLineAsync(
            _fixture.AppFactory.Services,
            line.SubscriberProfileId,
            line.MsisdnAssetId);

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        var tempFiles = new List<string>();

        try
        {
            (playwright, browser, _, var page) =
                await PlaywrightUiHelper.LaunchComprehensiveMasterPageAsync(_fixture.PublicBaseUrl);

            await PlaywrightUiHelper.LoginViaUiAsync(
                page,
                _fixture.PublicBaseUrl,
                PlaywrightUiHelper.ShowroomEmail,
                PlaywrightUiHelper.DemoPassword);
            await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
            await PlaywrightUiHelper.InstallExecutiveAlertsSnifferAsync(page, _fixture.PublicBaseUrl);

            // Core financial & provisioning flows
            await RunCase1WalletRechargeAsync(page, line, subscriptionId);
            await RunCase2PackageMigrationAsync(page, line, tempFiles);
            await RunCase3VasToggleIdempotencyAsync(page, line, subscriptionId);
            await RunCase4SupportTicketLifecycleAsync(page, line);

            // Re-auth showroom after call-center case
            await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
            await PlaywrightUiHelper.LoginViaUiAsync(
                page,
                _fixture.PublicBaseUrl,
                PlaywrightUiHelper.ShowroomEmail,
                PlaywrightUiHelper.DemoPassword);
            await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);

            // Network, CBS, timeline
            await RunCase5HlrQueryOnHealthyLineAsync(page, line);
            await RunCase12TimelineOperationsFilterAsync(page);
            await RunCase13NotProvisionedLineHlrDriftAsync(page, notProvisionedLine);

            // Extended wizard matrix (smoke + selective stateful)
            await RunCase6ChangeGsmWizardAsync(page, line, tempFiles);
            await RunCase7SimSwapWizardAsync(page, line);
            await RunCase8RefundWizardAsync(page, line);
            await RunCase9BadDebtWriteOffGatewayAsync(page, line);
            await RunCase10DeviceSaleCatalogAsync(page, line);
            await RunCase11ChangeNumberInternalAsync(page, line);
            await RunCase14ReconnectSuspendedLineAsync(page, suspendedLine, tempFiles);
            await RunCase15TerminationVoluntaryDraftAsync(page, line, tempFiles);
            await RunCase16SuspensionBillingDraftAsync(page, line, tempFiles);
            await RunCase17TakeoverWizardAsync(page, line);
            await RunCase18ActivationWizardCatalogAsync(page, line);
            await RunCase19TimelineAllFiltersAsync(page, line.CustomerId);
            await RunCase20ProfileBillingTabAsync(page);
            await RunCase21PortInChangeNumberAsync(page, line);
            await RunCase22FraudSuspensionBackOfficeAsync(page, line, tempFiles);
            await RunCase23MgrScheduledEffectiveDateAsync(page, line);
            await RunCase24DevInstallmentWizardAsync(page, line);
            await RunCase25TicketsTabResolvedAsync(page, line);
        }
        finally
        {
            foreach (var path in tempFiles.Where(File.Exists))
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // Best-effort temp cleanup.
                }
            }

            await PlaywrightUiHelper.DisposeAsync(playwright, browser);
        }
    }

    [Fact]
    public void ReleaseBuild_ProjectFile_IsPresentForCiCompilationGate()
    {
        var projectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ASPNET.E2E.Tests.csproj"));
        Assert.True(File.Exists(projectPath), $"E2E project not found: {projectPath}");
    }

    private async Task RunCase1WalletRechargeAsync(IPage page, ShowcaseLineRef line, string subscriptionId)
    {
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Services");
        await page.Locator(".c360-sub-card").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        await page.Locator(".fw-bold.text-success").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var balanceBefore = await ParseDisplayedBalanceAsync(page);
        var gatewayRef = $"E2E-C360-{Guid.NewGuid():N}"[..22];

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = new Regex("Recharge|شحن", RegexOptions.IgnoreCase),
        }).First.ClickAsync();

        await PlaywrightUiHelper.CompleteWalletRechargeSwalAsync(page, RechargeAmount, gatewayRef);
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page.Locator(".fw-bold.text-success").First).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        var balanceAfter = await ParseDisplayedBalanceAsync(page);
        if (balanceBefore.HasValue && balanceAfter.HasValue)
        {
            Assert.Equal(balanceBefore.Value + RechargeAmount, balanceAfter.Value);
        }

        await PlaywrightUiHelper.ClickC360TabAsync(page, "Timeline");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Payments" }).ClickAsync();
        var timelineTop = page.Locator(".list-group-item").First;
        await timelineTop.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var timelineText = await timelineTop.InnerTextAsync();
        Assert.Matches(new Regex("5[,.]?000|Recharge|Payment|Completed|دفع", RegexOptions.IgnoreCase), timelineText);

        var ledger = await E2ETestDataHelper.GetLatestCompletedRechargeByGatewayRefAsync(
            _fixture.AppFactory.Services,
            line.CustomerId,
            gatewayRef);
        Assert.NotNull(ledger);
        Assert.Equal(RechargeAmount, ledger!.Amount);
        Assert.Equal(PaymentTransactionStatus.Completed, ledger.Status);
        Assert.False(string.IsNullOrWhiteSpace(ledger.PaymentNumber));
        Assert.NotNull(ledger.ConfirmedAtUtc);

        var hasPaymentAudit = await E2ETestDataHelper.HasAuditLogForEntityAsync(
            _fixture.AppFactory.Services,
            nameof(TelecomPaymentTransaction),
            ledger.PaymentId);
        Assert.True(hasPaymentAudit || !string.IsNullOrWhiteSpace(ledger.ConfirmedByUserId));

        _ = await PlaywrightUiHelper.ReadExecutiveAlertCountAsync(page);
    }

    private async Task RunCase2PackageMigrationAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        var subscriptionId = await E2ETestDataHelper.ResolveSubscriptionIdForLineAsync(
            _fixture.AppFactory.Services,
            line.SubscriberProfileId,
            line.MsisdnAssetId);
        var currentCode = await E2ETestDataHelper.GetSubscriptionOfferingCodeAsync(
            _fixture.AppFactory.Services,
            subscriptionId);
        var targetCode = currentCode == "YA_HALA_PLUS" ? "YA_HALA_30" : "YA_HALA_PLUS";

        IResponse? prorationResponse = null;
        page.Response += (_, response) =>
        {
            if (response.Url.Contains("GetMigrationProrationPreview", StringComparison.OrdinalIgnoreCase))
            {
                prorationResponse = response;
            }
        };

        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "migrate",
            line.LineKey);

        await page.Locator("[data-testid='c360-mgr-offering-select']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });

        var offeringSelect = page.Locator("[data-testid='c360-mgr-offering-select']");
        var targetOption = offeringSelect.Locator("option").Filter(new LocatorFilterOptions
        {
            HasText = targetCode,
        });
        await targetOption.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        var offeringValue = await targetOption.First.GetAttributeAsync("value")
            ?? throw new InvalidOperationException($"Offering {targetCode} not found.");
        await offeringSelect.SelectOptionAsync(offeringValue);

        await Assertions.Expect(page.GetByText(new Regex("Financial preview|Prorated|Price difference", RegexOptions.IgnoreCase)))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        if (prorationResponse is not null)
        {
            Assert.True(prorationResponse.Ok, "GetMigrationProrationPreview returned non-success status.");
        }

        var wizard = page.Locator("#c360ProvWizardModal");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|Registration|التالي", RegexOptions.IgnoreCase),
        }).ClickAsync();

        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Save request|Create draft|حفظ", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await wizard.Locator("text=/OP-/").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var kycPath = CreateTempPdf(tempFiles);
        var fileInput = wizard.Locator("input[type='file']").First;
        if (await fileInput.CountAsync() > 0)
        {
            await fileInput.SetInputFilesAsync(kycPath);
        }

        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Mark document|Document|وثيقة", RegexOptions.IgnoreCase),
        }).ClickAsync();

        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Confirm CBS|HLR|تأكيد", RegexOptions.IgnoreCase),
        }).ClickAsync();

        await wizard.Locator("text=/Confirmed|success|provision|تم/i").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });

        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Close|Done|إغلاق", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Services");
        await Assertions.Expect(page.Locator(".c360-sub-card").Filter(new LocatorFilterOptions
        {
            HasText = targetCode,
        }).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });

        await PlaywrightUiHelper.ClickC360TabAsync(page, "Timeline");
        var migrationEntry = page.Locator(".list-group-item").Filter(new LocatorFilterOptions
        {
            HasTextRegex = new Regex("Migration|ترحيل|OP-", RegexOptions.IgnoreCase),
        });
        await migrationEntry.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
    }

    private async Task RunCase3VasToggleIdempotencyAsync(
        IPage page,
        ShowcaseLineRef line,
        string subscriptionId)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "addpackage",
            line.LineKey);

        await page.Locator("[data-testid='c360-vas-wizard']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await SelectVasServiceAsync(page, VasServiceCode);
        await page.Locator("[data-testid='c360-vas-action-select']").SelectOptionAsync("Activate");

        var wizard = page.Locator("#c360ProvWizardModal");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|VAS|Activate|التالي", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await page.Locator(".swal2-success").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 120_000,
        });
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        var activeCount = await E2ETestDataHelper.CountActiveSubscriberVasRowsAsync(
            _fixture.AppFactory.Services,
            subscriptionId,
            VasServiceCode);
        Assert.Equal(1, activeCount);

        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "addpackage",
            line.LineKey);
        await SelectVasServiceAsync(page, VasServiceCode);
        await page.Locator("[data-testid='c360-vas-action-select']").SelectOptionAsync("Activate");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|VAS|Activate|التالي", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        activeCount = await E2ETestDataHelper.CountActiveSubscriberVasRowsAsync(
            _fixture.AppFactory.Services,
            subscriptionId,
            VasServiceCode);
        Assert.Equal(1, activeCount);

        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "addpackage",
            line.LineKey);
        await SelectVasServiceAsync(page, VasServiceCode);
        await page.Locator("[data-testid='c360-vas-action-select']").SelectOptionAsync("Deactivate");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|VAS|Deactivate|التالي", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        var vasRow = await E2ETestDataHelper.GetSubscriberVasRowAsync(
            _fixture.AppFactory.Services,
            subscriptionId,
            VasServiceCode);
        Assert.NotNull(vasRow);
        Assert.Equal(SubscriberVasStatus.Suspended, vasRow!.Status);

        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Services");
        var vasBadge = page.Locator(".c360-vas-table").Locator(".badge").Filter(new LocatorFilterOptions
        {
            HasTextRegex = new Regex("Inactive|Suspended|Deactivated|غير|موقوف", RegexOptions.IgnoreCase),
        });
        if (await vasBadge.CountAsync() > 0)
        {
            await Assertions.Expect(vasBadge.First).ToBeVisibleAsync();
        }
    }

    private async Task RunCase4SupportTicketLifecycleAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.LogoutViaUiAsync(page, _fixture.PublicBaseUrl);
        await PlaywrightUiHelper.LoginViaUiAsync(
            page,
            _fixture.PublicBaseUrl,
            PlaywrightUiHelper.CallCenterEmail,
            PlaywrightUiHelper.DemoPassword);
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = new Regex("Support ticket|تذكرة", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await page.Locator("[data-testid='c360-support-wizard']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        await page.Locator("[data-testid='c360-support-issue-type']").SelectOptionAsync("0");
        await page.Locator("[data-testid='c360-support-wizard'] textarea")
            .FillAsync("4G LTE Dropouts — sector Mazzeh speed below 1 Mbps.");

        var wizard = page.Locator("#c360ProvWizardModal");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Create|إنشاء", RegexOptions.IgnoreCase),
        }).ClickAsync();

        await wizard.Locator("strong").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });
        var ticketNumber = (await wizard.Locator("strong").First.InnerTextAsync()).Trim();
        Assert.Matches(new Regex("TT-|TKT-", RegexOptions.IgnoreCase), ticketNumber);

        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Close|Done|إغلاق", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await PlaywrightUiHelper.WaitForSwalToCloseAsync(page);

        var ticketId = await E2ETestDataHelper.GetTechnicalTicketIdByNumberAsync(
            _fixture.AppFactory.Services,
            ticketNumber);
        Assert.False(string.IsNullOrWhiteSpace(ticketId));

        await E2ETestDataHelper.TransitionTechnicalTicketStatusAsync(
            _fixture.AppFactory.Services,
            ticketId!,
            TechnicalTicketStatus.InProgress);

        await PlaywrightUiHelper.ClickC360TabAsync(page, "Timeline");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Tickets" }).ClickAsync();

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
        Assert.Contains("InProgress", statusText, StringComparison.OrdinalIgnoreCase);
    }

    private async Task RunCase5HlrQueryOnHealthyLineAsync(IPage page, ShowcaseLineRef line)
    {
        await SelectC360LineAsync(page, line.Msisdn);
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Services");

        var subCard = page.Locator(".c360-sub-card").Filter(new LocatorFilterOptions { HasText = line.Msisdn });
        await subCard.First.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Query HLR|HLR|استعلام", RegexOptions.IgnoreCase),
        }).ClickAsync();

        await subCard.First.Locator(".small.border-top").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var hlrText = await subCard.First.Locator(".small.border-top").InnerTextAsync();
        Assert.Matches(new Regex("ACTIVE|Active|نشط", RegexOptions.IgnoreCase), hlrText);
    }

    private async Task RunCase6ChangeGsmWizardAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "changeGsm",
            line.LineKey);

        await page.Locator("[data-testid='c360-cgt-wizard']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        await page.Locator("[data-testid='c360-cgt-target-type']").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await page.Locator("[data-testid='c360-cgt-migration-reason']").FillAsync("E2E CGT — prepaid to postpaid path review");
        await page.Locator("[data-testid='c360-cgt-migration-path']").SelectOptionAsync("Immediate");
        await page.Locator("[data-testid='c360-cgt-payment-ref']").FillAsync($"CGT-E2E-{Guid.NewGuid():N}"[..16]);

        var kycPath = CreateTempPdf(tempFiles);
        await page.Locator("[data-testid='c360-cgt-identity-upload']").SetInputFilesAsync(kycPath);

        await Assertions.Expect(page.Locator("[data-testid='c360-effective-date-block']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase7SimSwapWizardAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "simswap",
            line.LineKey);

        await page.Locator("[data-testid='c360-sim-replacement-reason']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await page.Locator("[data-testid='c360-sim-replacement-reason']").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await Assertions.Expect(page.Locator("[data-testid='c360-effective-date-block']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase8RefundWizardAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "refund",
            line.LineKey);

        await page.Locator("[data-testid='c360-effective-date-block']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await page.Locator("[data-testid='c360-effective-mode']").SelectOptionAsync("immediate");
        await Assertions.Expect(page.Locator("[data-testid='c360-rfd-deposit-snapshot']").Or(page.Locator("#c360ProvWizardModal")))
            .ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase9BadDebtWriteOffGatewayAsync(IPage page, ShowcaseLineRef line)
    {
        var debtLine = await E2ETestDataHelper.ResolveShowcaseLineAsync(
            _fixture.AppFactory.Services,
            TelecomDemoMsisdn.DebtSubscriber);
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, debtLine.CustomerId);
        await SelectC360LineAsync(page, debtLine.Msisdn);

        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            debtLine.CustomerId,
            "badDebt",
            debtLine.LineKey);

        await page.Locator("[data-testid='c360-bdr-collection-action']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await page.Locator("[data-testid='c360-bdr-collection-action']").SelectOptionAsync("WriteOffPartial");
        await Assertions.Expect(page.Locator("[data-testid='c360-bdr-writeoff-amount']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);

        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
    }

    private async Task RunCase10DeviceSaleCatalogAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "deviceSale",
            line.LineKey);

        await page.Locator("[data-testid='c360-dev-wizard']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var inventorySelect = page.Locator("[data-testid='c360-dev-inventory-select']");
        await inventorySelect.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var optionCount = await inventorySelect.Locator("option").CountAsync();
        Assert.True(optionCount > 1, "Device inventory catalog should expose at least one device.");
        await page.Locator("[data-testid='c360-dev-sale-type']").SelectOptionAsync("Cash");
        await Assertions.Expect(page.Locator("[data-testid='c360-effective-date-block']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase11ChangeNumberInternalAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "changeNumber",
            line.LineKey);

        await page.Locator("[data-testid='c360-cn-change-mode']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await page.Locator("[data-testid='c360-cn-change-mode']").SelectOptionAsync("Internal");
        await Assertions.Expect(page.Locator(".msisdn-picker-row").Or(page.Locator("[data-testid='c360-effective-date-block']")))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase12TimelineOperationsFilterAsync(IPage page)
    {
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Timeline");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Operations" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var items = page.Locator(".list-group-item");
        if (await items.CountAsync() > 0)
        {
            var firstText = await items.First.InnerTextAsync();
            Assert.Matches(new Regex("OP-|Migration|Operation|عملية", RegexOptions.IgnoreCase), firstText);
        }
    }

    private async Task RunCase13NotProvisionedLineHlrDriftAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await SelectC360LineAsync(page, line.Msisdn);
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Services");

        var subCard = page.Locator(".c360-sub-card").Filter(new LocatorFilterOptions { HasText = line.Msisdn });
        await subCard.First.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Query HLR|HLR|استعلام", RegexOptions.IgnoreCase),
        }).ClickAsync();

        await subCard.First.Locator(".small.border-top").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var hlrText = await subCard.First.InnerTextAsync();
        Assert.Matches(new Regex("NOT_PROVISIONED|Not provisioned|غير مزود|CRM mismatch|Drift", RegexOptions.IgnoreCase), hlrText);
    }

    private async Task RunCase14ReconnectSuspendedLineAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, line.CustomerId);
        await SelectC360LineAsync(page, line.Msisdn);

        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "reconnect",
            line.LineKey);

        await page.Locator("[data-testid='c360-rcn-clearance-type']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await page.Locator("[data-testid='c360-rcn-clearance-type']").SelectOptionAsync("Operational");
        await page.Locator("#c360ProvWizardModal input[type='text']").First.FillAsync("E2E operational reconnect after suspension");

        await page.Locator("#c360ProvWizardModal .alert").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var wizard = page.Locator("#c360ProvWizardModal");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|Registration|التالي", RegexOptions.IgnoreCase),
        }).ClickAsync();

        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Save request|Create draft|حفظ", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await wizard.Locator("text=/OP-/").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            _fixture.AppFactory.Services,
            line.SubscriberProfileId,
            TelecomOperationKind.Reconnect);
        Assert.NotNull(op);
        Assert.Matches(new Regex("^OP-", RegexOptions.IgnoreCase), op!.Number);

        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase15TerminationVoluntaryDraftAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await SelectC360LineAsync(page, line.Msisdn);
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "termination",
            line.LineKey);

        await page.Locator("[data-testid='c360-trm-termination-type']").SelectOptionAsync("Voluntary");
        await page.Locator("#c360ProvWizardModal input[type='text']").First.FillAsync("E2E voluntary termination draft");

        var wizard = page.Locator("#c360ProvWizardModal");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|Registration|التالي", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Save request|Create draft|حفظ", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await wizard.Locator("text=/OP-/").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            _fixture.AppFactory.Services,
            line.SubscriberProfileId,
            TelecomOperationKind.Termination);
        Assert.NotNull(op);
        Assert.NotEqual(TelecomOperationStatus.Failed, op!.Status);

        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase16SuspensionBillingDraftAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "suspension",
            line.LineKey);

        await page.Locator("[data-testid='c360-sus-suspension-type']").SelectOptionAsync("Billing");
        await page.Locator("#c360ProvWizardModal input[type='text']").First.FillAsync("E2E billing suspension draft");
        await Assertions.Expect(page.Locator("[data-testid='c360-sus-fraud-clearance']")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator("[data-testid='c360-effective-date-block']")).ToBeVisibleAsync();

        var wizard = page.Locator("#c360ProvWizardModal");
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Next|Registration|التالي", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await wizard.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions
        {
            NameRegex = new Regex("Save request|Create draft|حفظ", RegexOptions.IgnoreCase),
        }).ClickAsync();
        await wizard.Locator("text=/OP-/").First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var op = await E2ETestDataHelper.GetLatestTelecomOperationByKindAsync(
            _fixture.AppFactory.Services,
            line.SubscriberProfileId,
            TelecomOperationKind.TemporarySuspension);
        Assert.NotNull(op);

        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase17TakeoverWizardAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "takeover",
            line.LineKey);

        await page.Locator("[data-testid='c360-tko-wizard']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Assertions.Expect(page.Locator("[data-testid='c360-tko-deposit-policy']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase18ActivationWizardCatalogAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(
            page,
            _fixture.PublicBaseUrl,
            line.CustomerId,
            "activate");

        await page.Locator("[data-testid='c360-act-line-type']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var lineTypeCount = await page.Locator("[data-testid='c360-act-line-type'] option").CountAsync();
        Assert.True(lineTypeCount > 1, "Activation wizard should load telecom line types.");
        await Assertions.Expect(page.Locator("[data-testid='c360-effective-date-block']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase19TimelineAllFiltersAsync(IPage page, string customerId)
    {
        await PlaywrightUiHelper.GotoC360ProfileAsync(page, _fixture.PublicBaseUrl, customerId);
        await PlaywrightBssWizardHelper.RunC360TimelineFiltersAsync(page, TelecomBssWizardScenarioCatalog.TimelineFilterLabels);
    }

    private async Task RunCase20ProfileBillingTabAsync(IPage page)
    {
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Profile");
        await Assertions.Expect(page.Locator(".telecom-bento-card").Filter(new LocatorFilterOptions
        {
            HasTextRegex = new Regex("billing|CBS|فوترة", RegexOptions.IgnoreCase),
        }).First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 60_000 });
    }

    private async Task RunCase21PortInChangeNumberAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "changeNumber", line.LineKey);
        await page.Locator("[data-testid='c360-cn-change-mode']").SelectOptionAsync("PortIn");
        await Assertions.Expect(page.Locator("[data-testid='c360-cn-port-in-msisdn']")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-testid='c360-cn-donor-operator']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase22FraudSuspensionBackOfficeAsync(
        IPage page,
        ShowcaseLineRef line,
        ICollection<string> tempFiles)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "suspension", line.LineKey);
        await page.Locator("[data-testid='c360-sus-suspension-type']").SelectOptionAsync(Application.Common.Telecom.Suspension.SuspensionWellKnown.Fraud);
        await Assertions.Expect(page.Locator("[data-testid='c360-sus-fraud-clearance']")).ToBeVisibleAsync();
        await page.Locator("#c360ProvWizardModal input[type='file']").Last.SetInputFilesAsync(CreateTempPdf(tempFiles));
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase23MgrScheduledEffectiveDateAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "migrate", line.LineKey);
        await page.Locator("[data-testid='c360-effective-mode']").SelectOptionAsync("scheduled");
        await Assertions.Expect(page.Locator("[data-testid='c360-effective-date']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase24DevInstallmentWizardAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.GotoC360WizardAsync(page, _fixture.PublicBaseUrl, line.CustomerId, "deviceSale", line.LineKey);
        await page.Locator("[data-testid='c360-dev-sale-type']").SelectOptionAsync("Installment");
        await Assertions.Expect(page.Locator("[data-testid='c360-dev-installment-plan']")).ToBeVisibleAsync();
        await PlaywrightUiHelper.CloseC360WizardAsync(page);
    }

    private async Task RunCase25TicketsTabResolvedAsync(IPage page, ShowcaseLineRef line)
    {
        await PlaywrightUiHelper.ClickC360TabAsync(page, "Tickets");
        await page.Locator(".list-group-item, .table tbody tr, .c360-ticket-row").First.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    private static async Task SelectC360LineAsync(IPage page, string msisdn)
    {
        var value = await page.EvaluateAsync<string>(
            """
            (digits) => {
                const selects = document.querySelectorAll('select.form-select');
                for (const sel of selects) {
                    for (const opt of sel.options) {
                        if ((opt.textContent || '').includes(digits)) return opt.value;
                    }
                }
                return '';
            }
            """,
            msisdn);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        await page.Locator("select.form-select").First.SelectOptionAsync(value);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private static async Task SelectVasServiceAsync(IPage page, string serviceCode)
    {
        var value = await page.EvaluateAsync<string>(
            """
            (code) => {
                const sel = document.querySelector('[data-testid="c360-vas-service-select"]');
                if (!sel) return '';
                for (const opt of sel.options) {
                    if ((opt.textContent || '').includes(code)) return opt.value;
                }
                return '';
            }
            """,
            serviceCode);
        Assert.False(string.IsNullOrWhiteSpace(value), $"VAS option {serviceCode} not found.");
        await page.Locator("[data-testid='c360-vas-service-select']").SelectOptionAsync(value);
    }

    private static async Task<decimal?> ParseDisplayedBalanceAsync(IPage page)
    {
        var raw = await page.Locator(".fw-bold.text-success").First.InnerTextAsync();
        var digits = Regex.Replace(raw, @"[^\d.,]", "").Replace(",", "");
        return decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string CreateTempPdf(ICollection<string> registry)
    {
        var path = Path.Combine(Path.GetTempPath(), $"c360-master-kyc-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, "%PDF-1.4 C360 Master E2E KYC Placeholder", Encoding.UTF8);
        registry.Add(path);
        return path;
    }
}
