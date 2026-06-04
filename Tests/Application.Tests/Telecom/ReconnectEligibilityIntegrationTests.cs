using Application.Tests.Dashboard;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.Suspension;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class ReconnectEligibilityIntegrationTests
{
    [Fact]
    public async Task ValidateForCreateAsync_billing_suspended_with_payment_allowed()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedSuspendedLineAsync(
            ctx,
            SuspensionWellKnown.Billing,
            MsisdnPoolStatus.Suspended);
        var checker = new ReconnectEligibilityChecker(ctx, new StubBilling(0m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            "Settled invoice",
            ReconnectWellKnown.Payment,
            "PAY-DEMO-001",
            false,
            null);

        Assert.True(result.Allowed);
        Assert.False(result.RequiresBackOfficeApproval);
        Assert.Equal("Allowed", result.ValidationCode);
    }

    [Fact]
    public async Task ValidateForCreateAsync_fraud_requires_back_office_until_clearance()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedSuspendedLineAsync(
            ctx,
            SuspensionWellKnown.Fraud,
            MsisdnPoolStatus.Suspended);
        var checker = new ReconnectEligibilityChecker(ctx, new StubBilling(0m));

        var pending = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            "Investigation closed",
            ReconnectWellKnown.Fraud,
            null,
            false,
            null);

        Assert.False(pending.Allowed);
        Assert.True(pending.RequiresBackOfficeApproval);
        Assert.Equal("FraudClearanceRequired", pending.ValidationCode);

        var cleared = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            "Investigation closed",
            ReconnectWellKnown.Fraud,
            null,
            true,
            null);

        Assert.True(cleared.Allowed);
        Assert.True(cleared.RequiresBackOfficeApproval);
        Assert.Equal("BackOfficePending", cleared.ValidationCode);
    }

    [Fact]
    public async Task ValidateForCreateAsync_terminated_profile_denied_val0905()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedSuspendedLineAsync(
            ctx,
            SuspensionWellKnown.CustomerRequest,
            MsisdnPoolStatus.Suspended);

        var profile = await ctx.SubscriberProfile.FindAsync(profileId);
        profile!.Terminate();
        await ctx.SaveChangesAsync();

        var checker = new ReconnectEligibilityChecker(ctx, new StubBilling(0m));
        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            "Retry",
            ReconnectWellKnown.Customer,
            null,
            false,
            null);

        Assert.False(result.Allowed);
        Assert.Contains("VAL-09-05", result.MessageAr);
        Assert.Equal("RequiresNewActivation", result.ValidationCode);
    }

    [Fact]
    public async Task ValidateForCreateAsync_throws_when_outstanding_debt_on_payment_clearance()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedSuspendedLineAsync(
            ctx,
            SuspensionWellKnown.Billing,
            MsisdnPoolStatus.Suspended);
        var checker = new ReconnectEligibilityChecker(ctx, new StubBilling(-1200m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            "Pay",
            ReconnectWellKnown.Payment,
            "PAY-1",
            false,
            null);

        Assert.False(result.Allowed);
        Assert.Contains("VAL-09-02", result.MessageAr);
        Assert.Equal("OutstandingDebt", result.ValidationCode);
    }

    [Fact]
    public void ReconnectEligibilityMatrix_maps_regulatory_to_back_office()
    {
        var matrix = ReconnectEligibilityMatrix.Evaluate(new ReconnectEligibilityMatrixInput(
            SubscriberOperationalStatus.Suspended,
            CustomerStatus.Active,
            MsisdnPoolStatus.Suspended,
            SuspensionWellKnown.Regulatory,
            ReconnectWellKnown.Customer,
            false,
            0m,
            false));

        Assert.False(matrix.Allowed);
        Assert.True(matrix.RequiresBackOfficeApproval);
        Assert.Equal("FraudClearanceRequired", matrix.ValidationCode);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options);
    }

    private static async Task<(string ProfileId, string AssetId, string Msisdn)> SeedSuspendedLineAsync(
        QueryContext ctx,
        string suspensionType,
        MsisdnPoolStatus poolStatus)
    {
        var customerId = Guid.NewGuid().ToString();
        var customer = IndividualCustomer.Create(
            "مشترك موقوف",
            "ACC-SUS-01",
            "12345678902",
            PostalAddress.Empty,
            null,
            null,
            null,
            null);
        customer.Id = customerId;
        customer.IsDeleted = false;

        var profileId = Guid.NewGuid().ToString();
        var profile = new SubscriberProfile
        {
            Id = profileId,
            CustomerId = customerId,
            IsDeleted = false,
            OperationalStatus = SubscriberOperationalStatus.Suspended,
        };

        var assetId = Guid.NewGuid().ToString();
        var msisdn = "0995550001";
        var asset = new MsisdnAsset
        {
            Id = assetId,
            Msisdn = msisdn,
            IsDeleted = false,
            SubscriberProfileId = profileId,
        };
        asset.TransitionTo(MsisdnPoolStatus.Active);
        if (poolStatus == MsisdnPoolStatus.Suspended)
        {
            asset.TransitionTo(MsisdnPoolStatus.Suspended);
        }

        var susOpId = Guid.NewGuid().ToString();
        var susOp = new TelecomOperationRequest
        {
            Id = susOpId,
            Kind = TelecomOperationKind.TemporarySuspension,
            Number = "SUS-TEST-1",
            Status = TelecomOperationStatus.Completed,
            DocumentStatus = TelecomDocumentStatus.Verified,
            SubscriberProfileId = profileId,
            MsisdnAssetId = assetId,
            SuspensionType = suspensionType,
            SuspensionReason = "Test",
            IsDeleted = false,
            ConfirmedAtUtc = DateTime.UtcNow.AddDays(-1),
        };

        ctx.Customer.Add(customer);
        ctx.SubscriberProfile.Add(profile);
        ctx.MsisdnAsset.Add(asset);
        ctx.TelecomOperationRequest.Add(susOp);
        await ctx.SaveChangesAsync();

        return (profileId, assetId, msisdn);
    }

    private sealed class StubBilling(decimal balance) : IBillingSystemIntegration
    {
        public Task<decimal> GetOutstandingBalanceAsync(string msisdn, CancellationToken cancellationToken = default) =>
            Task.FromResult(balance);

        public Task<BillingProvisionResult> ProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<BillingProvisionResult> ReverseProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<BillingRechargeResult> RechargeAsync(BillingRechargeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<BillingRechargeResult> ReverseRechargeAsync(BillingReverseRechargeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
