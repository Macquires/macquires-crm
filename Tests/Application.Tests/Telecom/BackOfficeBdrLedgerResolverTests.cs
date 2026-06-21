using Application.Common.Telecom.BackOffice;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.Reconnect;
using Application.Tests.Dashboard;
using Application.Tests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class BackOfficeBdrLedgerResolverTests
{
    [Fact]
    public async Task Resolve_manual_write_off_returns_ManualDiscount_ledger()
    {
        await using var ctx = CreateContext();
        var (_, assetId, _) = await SeedLineAsync(ctx);
        var resolver = CreateResolver(ctx, new StubBilling(-15_000m), alwaysValidPayment: true);

        var manualBdr = CreateBdr(
            assetId,
            BadDebtWellKnown.WriteOffPartial,
            outstanding: -15_000m,
            writeOff: 10_000m,
            cash: 5_000m,
            notes: "demo=WriteOffPartial");

        var view = await resolver.ResolveAsync(manualBdr);

        Assert.Equal(BackOfficeBdrLedgerModes.ManualDiscount, view.LedgerMode);
        Assert.Equal(15_000m, view.OutstandingDebt);
        Assert.Equal(10_000m, view.WriteOffWaiver);
        Assert.Equal(5_000m, view.CashCollection);
    }

    [Fact]
    public async Task Resolve_auto_settlement_returns_FullPaymentSettlement_with_zero_debt_and_waiver()
    {
        await using var ctx = CreateContext();
        var (_, assetId, _) = await SeedLineAsync(ctx);
        var resolver = CreateResolver(ctx, new StubBilling(0m), alwaysValidPayment: true);

        var autoBdr = CreateBdr(
            assetId,
            BadDebtWellKnown.PaymentRecorded,
            outstanding: 0m,
            writeOff: 0m,
            cash: 15_000m,
            notes: $"{BackOfficeBdrLedgerResolver.AutoSettlementNoteMarker}|rcn=RCN-0003|ref=RCPT-2002");

        var view = await resolver.ResolveAsync(autoBdr);

        Assert.Equal(BackOfficeBdrLedgerModes.FullPaymentSettlement, view.LedgerMode);
        Assert.Equal(0m, view.OutstandingDebt);
        Assert.Equal(0m, view.WriteOffWaiver);
        Assert.Equal(15_000m, view.CashCollection);
    }

    [Fact]
    public async Task ShouldSuppressManualBdr_when_paid_rcn_clearance_exists()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedLineAsync(ctx);
        await SeedPaidReconnectAsync(ctx, profileId, assetId, "RCN-0003", "RCPT-2002");

        var resolver = CreateResolver(ctx, new StubBilling(0m), alwaysValidPayment: true);
        var manualBdr = CreateBdr(
            assetId,
            BadDebtWellKnown.WriteOffPartial,
            outstanding: -15_000m,
            writeOff: 10_000m,
            cash: 5_000m,
            notes: "demo=WriteOffPartial");

        var suppress = await resolver.ShouldSuppressManualBdrInQueueAsync(manualBdr);

        Assert.True(suppress);
    }

    [Fact]
    public async Task ShouldSuppressManualBdr_false_when_no_paid_rcn()
    {
        await using var ctx = CreateContext();
        var (_, assetId, _) = await SeedLineAsync(ctx);
        var resolver = CreateResolver(ctx, new StubBilling(-15_000m), alwaysValidPayment: true);

        var manualBdr = CreateBdr(
            assetId,
            BadDebtWellKnown.WriteOffPartial,
            outstanding: -15_000m,
            writeOff: 10_000m,
            cash: 5_000m,
            notes: "demo=WriteOffPartial");

        var suppress = await resolver.ShouldSuppressManualBdrInQueueAsync(manualBdr);

        Assert.False(suppress);
    }

    private static BackOfficeBdrLedgerResolver CreateResolver(
        QueryContext ctx,
        StubBilling billing,
        bool alwaysValidPayment)
    {
        var paymentValidator = new StubPaymentValidator(alwaysValidPayment);
        return new BackOfficeBdrLedgerResolver(ctx, billing, paymentValidator);
    }

    private static TelecomOperationRequest CreateBdr(
        string assetId,
        string collectionAction,
        decimal outstanding,
        decimal writeOff,
        decimal cash,
        string notes)
    {
        return new TelecomOperationRequest
        {
            Id = Guid.NewGuid().ToString(),
            Kind = TelecomOperationKind.BadDebtRecovery,
            Number = "BDR-TEST",
            Status = TelecomOperationStatus.PendingDocuments,
            SubscriberProfileId = Guid.NewGuid().ToString(),
            MsisdnAssetId = assetId,
            CollectionAction = collectionAction,
            OutstandingBalanceSnapshot = outstanding,
            WriteOffAmount = writeOff,
            CollectedAmount = cash,
            Notes = notes,
            IsDeleted = false,
        };
    }

    private static async Task SeedPaidReconnectAsync(
        QueryContext ctx,
        string profileId,
        string assetId,
        string rcnNumber,
        string paymentReference)
    {
        ctx.TelecomOperationRequest.Add(new TelecomOperationRequest
        {
            Id = Guid.NewGuid().ToString(),
            Kind = TelecomOperationKind.Reconnect,
            Number = rcnNumber,
            Status = TelecomOperationStatus.Paid_Pending_BackOffice_Clearance,
            ClearanceType = ReconnectWellKnown.Payment,
            PaymentReference = paymentReference,
            SubscriberProfileId = profileId,
            MsisdnAssetId = assetId,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private static async Task<(string ProfileId, string AssetId, IndividualCustomer Customer)> SeedLineAsync(QueryContext ctx)
    {
        var customer = IndividualCustomer.Create(
            "مازن المديون",
            "ACC-BDR",
            "12345678903",
            PostalAddress.Empty,
            null,
            null,
            null,
            null);
        customer.Id = Guid.NewGuid().ToString();
        customer.IsDeleted = false;

        var profileId = Guid.NewGuid().ToString();
        ctx.SubscriberProfile.Add(new SubscriberProfile
        {
            Id = profileId,
            CustomerId = customer.Id,
            IsDeleted = false,
            OperationalStatus = SubscriberOperationalStatus.Active,
        });

        var assetId = Guid.NewGuid().ToString();
        var asset = new MsisdnAsset
        {
            Id = assetId,
            Msisdn = "0939000002",
            IsDeleted = false,
            SubscriberProfileId = profileId,
        };
        asset.TransitionTo(MsisdnPoolStatus.Active);

        ctx.Customer.Add(customer);
        ctx.MsisdnAsset.Add(asset);
        await ctx.SaveChangesAsync();

        return (profileId, assetId, customer);
    }

    private sealed class StubBilling(decimal balance) : Application.Common.Integrations.IBillingSystemIntegration
    {
        public Task<decimal> GetOutstandingBalanceAsync(string msisdn, CancellationToken cancellationToken = default) =>
            Task.FromResult(balance);

        public Task AdjustBalanceAsync(string msisdn, decimal newBalance, string? reason = null, string? idempotencyKey = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Application.Common.Integrations.BillingProvisionResult> ProvisionAsync(
            Application.Common.Integrations.BillingProvisionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Application.Common.Integrations.BillingProvisionResult> ReverseProvisionAsync(
            Application.Common.Integrations.BillingProvisionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Application.Common.Integrations.BillingRechargeResult> RechargeAsync(
            Application.Common.Integrations.BillingRechargeRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Application.Common.Integrations.BillingRechargeResult> ReverseRechargeAsync(
            Application.Common.Integrations.BillingReverseRechargeRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class StubPaymentValidator(bool isValid) : IBackOfficePaymentReferenceValidator
    {
        public Task<BackOfficePaymentValidationResult> ValidateReconnectPaymentAsync(
            TelecomOperationRequest operation,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BackOfficePaymentValidationResult(isValid, isValid ? "OK" : "Invalid"));
    }
}
