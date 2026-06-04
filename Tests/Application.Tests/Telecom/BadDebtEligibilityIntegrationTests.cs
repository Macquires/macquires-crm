using Application.Tests.Dashboard;
using Application.Common.Telecom.BadDebt;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class BadDebtEligibilityIntegrationTests
{
    [Fact]
    public async Task ValidateForCreateAsync_payment_recorded_allowed_with_negative_balance()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedLineAsync(ctx, SubscriberOperationalStatus.Active, MsisdnPoolStatus.Active);
        var checker = new BadDebtEligibilityChecker(ctx, new StubBilling(-15_000m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            BadDebtWellKnown.PaymentRecorded,
            BadDebtWellKnown.Reminder1,
            "PAY-BDR-001",
            5000m,
            null,
            false,
            null);

        Assert.True(result.Allowed);
        Assert.False(result.RequiresBackOfficeApproval);
        Assert.Equal("Allowed", result.ValidationCode);
        Assert.Equal(-15_000m, result.OutstandingBalanceSnapshot);
    }

    [Fact]
    public async Task ValidateForCreateAsync_write_off_requires_back_office_pending()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedLineAsync(ctx, SubscriberOperationalStatus.Active, MsisdnPoolStatus.Active);
        var checker = new BadDebtEligibilityChecker(ctx, new StubBilling(-15_000m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            BadDebtWellKnown.WriteOffPartial,
            BadDebtWellKnown.WriteOffPending,
            null,
            null,
            10_000m,
            false,
            null);

        Assert.True(result.Allowed);
        Assert.True(result.RequiresBackOfficeApproval);
        Assert.Equal("BackOfficePending", result.ValidationCode);
    }

    [Fact]
    public async Task ValidateForCreateAsync_denies_when_no_outstanding_debt()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedLineAsync(ctx, SubscriberOperationalStatus.Active, MsisdnPoolStatus.Active);
        var checker = new BadDebtEligibilityChecker(ctx, new StubBilling(1000m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            BadDebtWellKnown.PaymentRecorded,
            BadDebtWellKnown.Reminder1,
            "PAY-1",
            100m,
            null,
            false,
            null);

        Assert.False(result.Allowed);
        Assert.Contains("VAL-16-02", result.MessageAr);
    }

    [Fact]
    public async Task ValidateForCreateAsync_blacklisted_customer_denied()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, customer) = await SeedLineAsync(ctx, SubscriberOperationalStatus.Active, MsisdnPoolStatus.Active);
        customer.Blacklist(CustomerStatusReasonCode.Fraud, "test");
        await ctx.SaveChangesAsync();

        var checker = new BadDebtEligibilityChecker(ctx, new StubBilling(-500m));
        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            BadDebtWellKnown.PaymentRecorded,
            null,
            "PAY-1",
            100m,
            null,
            false,
            null);

        Assert.False(result.Allowed);
        Assert.Equal("Blacklisted", result.ValidationCode);
    }

    [Fact]
    public async Task ValidateForCreateAsync_blocks_duplicate_open_bdr()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedLineAsync(ctx, SubscriberOperationalStatus.Active, MsisdnPoolStatus.Active);

        ctx.TelecomOperationRequest.Add(new TelecomOperationRequest
        {
            Id = Guid.NewGuid().ToString(),
            Kind = TelecomOperationKind.BadDebtRecovery,
            Number = "BDR-OPEN-1",
            Status = TelecomOperationStatus.PendingDocuments,
            DocumentStatus = TelecomDocumentStatus.Uploaded,
            SubscriberProfileId = profileId,
            MsisdnAssetId = assetId,
            CollectionAction = BadDebtWellKnown.WriteOffPartial,
            IsDeleted = false,
        });
        await ctx.SaveChangesAsync();

        var checker = new BadDebtEligibilityChecker(ctx, new StubBilling(-500m));

        await Assert.ThrowsAsync<Application.Common.Exceptions.BusinessRuleViolationException>(() =>
            checker.ValidateForCreateAsync(
                profileId,
                assetId,
                BadDebtWellKnown.PaymentRecorded,
                null,
                "PAY-2",
                100m,
                null,
                false,
                null));
    }

    [Fact]
    public async Task ValidateForCreateAsync_denies_when_profile_terminated()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedLineAsync(
            ctx,
            SubscriberOperationalStatus.Terminated,
            MsisdnPoolStatus.Active);
        var checker = new BadDebtEligibilityChecker(ctx, new StubBilling(-15_000m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            BadDebtWellKnown.PaymentRecorded,
            BadDebtWellKnown.Reminder1,
            "PAY-TERM",
            100m,
            null,
            false);

        Assert.False(result.Allowed);
        Assert.Contains("VAL-16-01", result.MessageAr);
        Assert.Equal("Terminated", result.ValidationCode);
    }

    [Fact]
    public void BadDebtEligibilityMatrix_hard_bar_requires_prior_soft_bar_or_reminder2()
    {
        var denied = BadDebtEligibilityMatrix.Evaluate(new BadDebtEligibilityMatrixInput(
            SubscriberOperationalStatus.Active,
            CustomerStatus.Active,
            BadDebtWellKnown.DunningEscalation,
            BadDebtWellKnown.HardBar,
            BadDebtWellKnown.Reminder1,
            false,
            null,
            null,
            -5000m,
            false));

        Assert.False(denied.Allowed);
        Assert.Equal("HardBarSequenceRequired", denied.ValidationCode);

        var allowed = BadDebtEligibilityMatrix.Evaluate(new BadDebtEligibilityMatrixInput(
            SubscriberOperationalStatus.Active,
            CustomerStatus.Active,
            BadDebtWellKnown.DunningEscalation,
            BadDebtWellKnown.HardBar,
            BadDebtWellKnown.Reminder2,
            false,
            null,
            null,
            -5000m,
            false));

        Assert.True(allowed.Allowed);
    }

    [Fact]
    public void BadDebtEligibilityMatrix_payment_without_reference_denied()
    {
        var matrix = BadDebtEligibilityMatrix.Evaluate(new BadDebtEligibilityMatrixInput(
            SubscriberOperationalStatus.Active,
            CustomerStatus.Active,
            BadDebtWellKnown.PaymentRecorded,
            BadDebtWellKnown.Reminder1,
            null,
            false,
            5000m,
            null,
            -1000m,
            false));

        Assert.False(matrix.Allowed);
        Assert.Equal("PaymentReferenceRequired", matrix.ValidationCode);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options);
    }

    private static async Task<(string ProfileId, string AssetId, IndividualCustomer Customer)> SeedLineAsync(
        QueryContext ctx,
        SubscriberOperationalStatus opStatus,
        MsisdnPoolStatus poolStatus)
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
        var profile = new SubscriberProfile
        {
            Id = profileId,
            CustomerId = customer.Id,
            IsDeleted = false,
            OperationalStatus = opStatus,
        };

        var assetId = Guid.NewGuid().ToString();
        var asset = new MsisdnAsset
        {
            Id = assetId,
            Msisdn = "0939000002",
            IsDeleted = false,
            SubscriberProfileId = profileId,
        };
        asset.TransitionTo(MsisdnPoolStatus.Active);
        if (poolStatus == MsisdnPoolStatus.Suspended)
        {
            asset.TransitionTo(MsisdnPoolStatus.Suspended);
        }

        ctx.Customer.Add(customer);
        ctx.SubscriberProfile.Add(profile);
        ctx.MsisdnAsset.Add(asset);
        await ctx.SaveChangesAsync();

        return (profileId, assetId, customer);
    }

    private sealed class StubBilling(decimal balance) : Application.Common.Integrations.IBillingSystemIntegration
    {
        public Task<decimal> GetOutstandingBalanceAsync(string msisdn, CancellationToken cancellationToken = default) =>
            Task.FromResult(balance);

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
}
