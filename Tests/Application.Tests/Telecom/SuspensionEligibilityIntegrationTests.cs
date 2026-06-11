using Application.Common.Integrations;
using Application.Common.Telecom.Suspension;
using Application.Tests.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class SuspensionEligibilityIntegrationTests
{
    [Fact]
    public async Task ValidateForCreateAsync_active_line_customer_request_allowed()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx);
        var checker = new SuspensionEligibilityChecker(ctx, new StubBilling(0m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            SuspensionWellKnown.CustomerRequest,
            "طلب عميل",
            SuspensionWellKnown.BarringFull,
            false,
            null);

        Assert.True(result.Allowed);
        Assert.False(result.RequiresBackOfficeApproval);
    }

    [Fact]
    public async Task ValidateForCreateAsync_fraud_requires_back_office()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx);
        var checker = new SuspensionEligibilityChecker(ctx, new StubBilling(0m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            SuspensionWellKnown.Fraud,
            "تحقيق",
            SuspensionWellKnown.BarringFull,
            false,
            null);

        Assert.True(result.Allowed);
        Assert.True(result.RequiresBackOfficeApproval);
    }

    [Fact]
    public async Task ValidateForCreateAsync_terminated_profile_denied()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx);
        var profile = await ctx.SubscriberProfile.FindAsync(profileId);
        profile!.Terminate();
        await ctx.SaveChangesAsync();

        var checker = new SuspensionEligibilityChecker(ctx, new StubBilling(0m));
        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            SuspensionWellKnown.CustomerRequest,
            "محاولة",
            SuspensionWellKnown.BarringFull,
            false,
            null);

        Assert.False(result.Allowed);
        Assert.Contains("VAL-08", result.MessageAr);
    }

    [Fact]
    public async Task ValidateForCreateAsync_auto_reconnect_end_date_cannot_exceed_90_days()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx);
        var checker = new SuspensionEligibilityChecker(ctx, new StubBilling(0m));

        var ex = await Assert.ThrowsAsync<Application.Common.Exceptions.BusinessRuleViolationException>(
            () => checker.ValidateForCreateAsync(
                profileId,
                assetId,
                SuspensionWellKnown.CustomerRequest,
                "سفر",
                SuspensionWellKnown.BarringFull,
                true,
                DateTime.UtcNow.AddDays(91)));

        Assert.Contains("90", ex.Message);
    }

    [Fact]
    public async Task ValidateForCreateAsync_data_only_barring_allowed()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx);
        var checker = new SuspensionEligibilityChecker(ctx, new StubBilling(0m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            SuspensionWellKnown.CustomerRequest,
            "حظر بيانات",
            SuspensionWellKnown.BarringDataOnly,
            false,
            null);

        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task ValidateForCreateAsync_auto_reconnect_requires_end_date()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx);
        var checker = new SuspensionEligibilityChecker(ctx, new StubBilling(0m));

        await Assert.ThrowsAsync<Application.Common.Exceptions.BusinessRuleViolationException>(
            () => checker.ValidateForCreateAsync(
                profileId,
                assetId,
                SuspensionWellKnown.CustomerRequest,
                "مؤقت",
                SuspensionWellKnown.BarringFull,
                true,
                null));
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private static async Task<(string ProfileId, string AssetId, string Msisdn)> SeedActiveLineAsync(QueryContext ctx)
    {
        var customerId = Guid.NewGuid().ToString();
        var customer = IndividualCustomer.Create(
            "مشترك نشط",
            "ACC-SUS-ACTIVE",
            "12345678901",
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
            OperationalStatus = SubscriberOperationalStatus.Active,
        };

        var assetId = Guid.NewGuid().ToString();
        var msisdn = "0939111001";
        var asset = new MsisdnAsset
        {
            Id = assetId,
            Msisdn = msisdn,
            IsDeleted = false,
            SubscriberProfileId = profileId,
        };
        asset.TransitionTo(MsisdnPoolStatus.Active);

        ctx.Customer.Add(customer);
        ctx.SubscriberProfile.Add(profile);
        ctx.MsisdnAsset.Add(asset);
        await ctx.SaveChangesAsync();

        return (profileId, assetId, msisdn);
    }

    private sealed class StubBilling(decimal balance) : IBillingSystemIntegration
    {
        public Task<decimal> GetOutstandingBalanceAsync(string msisdn, CancellationToken cancellationToken = default) =>
            Task.FromResult(balance);

        public Task AdjustBalanceAsync(string msisdn, decimal newBalance, string? reason = null, string? idempotencyKey = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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
