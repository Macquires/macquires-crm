using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Telecom.Termination;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class TerminationEligibilityIntegrationTests
{
    [Fact]
    public async Task ValidateForCreateAsync_voluntary_individual_allowed_without_back_office()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx, CustomerKind.Individual);
        var checker = new TerminationEligibilityChecker(ctx, new StubBilling(0m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            TerminationWellKnown.Voluntary,
            "CustomerRequest",
            "Declined");

        Assert.True(result.Allowed);
        Assert.False(result.RequiresBackOfficeApproval);
        Assert.Equal("Allowed", result.ValidationCode);
    }

    [Fact]
    public async Task ValidateForCreateAsync_fraud_requires_back_office()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx, CustomerKind.Individual);
        var checker = new TerminationEligibilityChecker(ctx, new StubBilling(0m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            TerminationWellKnown.Fraud,
            "Investigation",
            null);

        Assert.True(result.Allowed);
        Assert.True(result.RequiresBackOfficeApproval);
        Assert.Equal("BackOfficePending", result.ValidationCode);
    }

    [Fact]
    public async Task ValidateForCreateAsync_denies_when_profile_already_terminated()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx, CustomerKind.Individual);
        var profile = await ctx.SubscriberProfile.FindAsync(profileId);
        profile!.Terminate();
        await ctx.SaveChangesAsync();

        var checker = new TerminationEligibilityChecker(ctx, new StubBilling(0m));
        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            TerminationWellKnown.Voluntary,
            "CustomerRequest",
            "Declined");

        Assert.False(result.Allowed);
        Assert.Contains("VAL-10-04", result.MessageAr);
        Assert.Equal("AlreadyTerminated", result.ValidationCode);
    }

    [Fact]
    public async Task ValidateForCreateAsync_throws_when_outstanding_debt()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx, CustomerKind.Individual);
        var checker = new TerminationEligibilityChecker(ctx, new StubBilling(-500m));

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            checker.ValidateForCreateAsync(
                profileId,
                assetId,
                TerminationWellKnown.Voluntary,
                "CustomerRequest",
                "Declined"));

        Assert.Contains("VAL-10-02", ex.Message);
    }

    [Fact]
    public async Task ValidateForCreateAsync_corporate_requires_back_office_even_voluntary()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _) = await SeedActiveLineAsync(ctx, CustomerKind.Corporate);
        var checker = new TerminationEligibilityChecker(ctx, new StubBilling(0m));

        var result = await checker.ValidateForCreateAsync(
            profileId,
            assetId,
            TerminationWellKnown.Voluntary,
            "ContractEnd",
            "Declined");

        Assert.True(result.Allowed);
        Assert.True(result.RequiresBackOfficeApproval);
    }

    private static QueryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options);
    }

    private static async Task<(string ProfileId, string AssetId, string Msisdn)> SeedActiveLineAsync(
        QueryContext ctx,
        CustomerKind kind)
    {
        var customerId = Guid.NewGuid().ToString();
        Customer customer = kind == CustomerKind.Corporate
            ? CorporateCustomer.Create(
                "شركة تجريبية",
                "ACC-CORP-01",
                "1234567890",
                PostalAddress.Empty,
                null,
                null,
                null,
                null)
            : IndividualCustomer.Create(
                "مشترك تجريبي",
                "ACC-IND-01",
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
        var msisdn = "0991234567";
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
