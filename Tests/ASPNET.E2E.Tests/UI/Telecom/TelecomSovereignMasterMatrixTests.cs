using ASPNET.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace ASPNET.E2E.Tests.UI.Telecom;

/// <summary>
/// Sovereign Carrier-Grade Master Matrix — 15 independent wizard E2E tests.
/// Each test grinds all 4 surfaces: POS Hub PBAC → Back-Office SSOT → C360 Timeline → Executive Pulse.
/// Run all: dotnet test --filter "Category=TelecomMasterMatrix"
/// Run one: dotnet test --filter "FullyQualifiedName~Test_03_SIM_Swap_Flow"
/// </summary>
[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "UI")]
[Trait("Category", "TelecomMasterMatrix")]
[Trait("Category", "Comprehensive")]
public sealed class TelecomSovereignMasterMatrixTests
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomSovereignMasterMatrixTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public Task Test_01_ACT_NewActivation_Flow() =>
        RunWizardTestAsync("ACT");

    [SkippableFact]
    public Task Test_02_MGR_PackageUpgrade_Flow() =>
        RunWizardTestAsync("MGR");

    [SkippableFact]
    public Task Test_03_SIM_Swap_Flow() =>
        RunWizardTestAsync("SIM");

    [SkippableFact]
    public Task Test_04_CNR_ChangeNumber_Flow() =>
        RunWizardTestAsync("CNR");

    [SkippableFact]
    public Task Test_05_PAY_RechargeAndLedger_Flow() =>
        RunWizardTestAsync("PAY");

    [SkippableFact]
    public Task Test_06_SUS_FraudSuspension_Flow() =>
        RunWizardTestAsync("SUS");

    [SkippableFact]
    public Task Test_07_CGT_ChangeGsmType_Flow() =>
        RunWizardTestAsync("CGT");

    [SkippableFact]
    public Task Test_08_BDR_BadDebtWriteOff_Flow() =>
        RunWizardTestAsync("BDR");

    [SkippableFact]
    public Task Test_09_TKO_TransferOwnership_Flow() =>
        RunWizardTestAsync("TKO");

    [SkippableFact]
    public Task Test_10_TRM_Termination_Flow() =>
        RunWizardTestAsync("TRM");

    [SkippableFact]
    public Task Test_11_RCN_OperationalReconnect_Flow() =>
        RunWizardTestAsync("RCN");

    [SkippableFact]
    public Task Test_12_RFD_FinancialRefund_Flow() =>
        RunWizardTestAsync("RFD");

    [SkippableFact]
    public Task Test_13_DEV_DeviceInstallment_Flow() =>
        RunWizardTestAsync("DEV");

    [SkippableFact]
    public Task Test_14_VAS_ValueAddedServices_Flow() =>
        RunWizardTestAsync("VAS");

    [SkippableFact]
    public Task Test_15_SUP_SupportTicket_Flow() =>
        RunWizardTestAsync("SUP");

    private async Task RunWizardTestAsync(string wizardCode)
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        Skip.If(string.IsNullOrWhiteSpace(_fixture.PublicBaseUrl), "Kestrel public URL not available.");

        await PlaywrightUiHelper.EnsureChromiumInstalledAsync();

        var wizard = PlaywrightMasterMatrixHelper.GetWizard(wizardCode);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        var tempFiles = new List<string>();

        try
        {
            (playwright, browser, _, var page) = wizardCode == "ACT"
                ? await PlaywrightUiHelper.LaunchPresentationPageAsync(_fixture.PublicBaseUrl)
                : await PlaywrightUiHelper.LaunchPageAsync(
                    _fixture.PublicBaseUrl,
                    headless: PlaywrightUiHelper.ResolveHeadless());

            await PlaywrightMasterMatrixHelper.RunMasterMatrixJourneyAsync(
                wizard,
                _fixture,
                page,
                _fixture.PublicBaseUrl,
                tempFiles);
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
}
