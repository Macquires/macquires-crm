using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Telecom.OfferSubscription;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class OfferSubscriptionEligibilityIntegrationTests
{
    [Fact]
    public async Task ValidateForMigrationCreateAsync_allows_compatible_offering()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, offeringId, productId) = await SeedLineWithCatalogAsync(ctx);
        var checker = new OfferSubscriptionEligibilityChecker(ctx, new StubBilling(0m));

        var result = await checker.ValidateForMigrationCreateAsync(
            profileId,
            assetId,
            offeringId,
            productId);

        Assert.True(result.Allowed);
        Assert.Equal("MigrationAllowed", result.ValidationCode);
    }

    [Fact]
    public async Task ValidateForMigrationCreateAsync_denies_same_offer()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, offeringId, productId) = await SeedLineWithCatalogAsync(ctx);
        var sub = await ctx.TelecomSubscription.FirstAsync(s => s.SubscriberProfileId == profileId);
        sub.ProductId = productId;
        sub.ProductOfferingId = offeringId;
        await ctx.SaveChangesAsync();

        var checker = new OfferSubscriptionEligibilityChecker(ctx, new StubBilling(0m));
        var result = await checker.ValidateForMigrationCreateAsync(
            profileId,
            assetId,
            offeringId,
            productId);

        Assert.False(result.Allowed);
        Assert.Contains("VAL-11-09", result.MessageAr);
    }

    [Fact]
    public async Task ValidateForMigrationCreateAsync_throws_on_debt()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, offeringId, productId) = await SeedLineWithCatalogAsync(ctx);
        var checker = new OfferSubscriptionEligibilityChecker(ctx, new StubBilling(-100m));

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            checker.ValidateForMigrationCreateAsync(profileId, assetId, offeringId, productId));

        Assert.Contains("VAL-11-12", ex.Message);
    }

    [Fact]
    public async Task ValidateForMigrationCreateAsync_denies_incompatible_postpaid_offering()
    {
        await using var ctx = CreateContext();
        var (profileId, assetId, _, productId) = await SeedLineWithCatalogAsync(ctx);
        var postpaidOfferingId = Guid.NewGuid().ToString();
        ctx.ProductOffering.Add(new ProductOffering
        {
            Id = postpaidOfferingId,
            Name = "باقة فاتورة فقط",
            Code = "POST_ONLY",
            IsActive = true,
            CompatibleSubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Postpaid,
            ProductId = productId,
            IsDeleted = false,
        });
        await ctx.SaveChangesAsync();

        var checker = new OfferSubscriptionEligibilityChecker(ctx, new StubBilling(0m));
        var result = await checker.ValidateForMigrationCreateAsync(
            profileId,
            assetId,
            postpaidOfferingId,
            productId);

        Assert.False(result.Allowed);
        Assert.Contains("VAL-11-08", result.MessageAr);
    }

    private static QueryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private static async Task<(string ProfileId, string AssetId, string OfferingId, string ProductId)> SeedLineWithCatalogAsync(
        QueryContext ctx)
    {
        var customerId = Guid.NewGuid().ToString();
        var customer = CorporateCustomer.Create(
            "شركة تجريبية",
            "ACC-01",
            "1234567890",
            PostalAddress.Empty,
            null,
            null,
            null,
            null);
        customer.Id = customerId;
        customer.IsDeleted = false;

        var profileId = Guid.NewGuid().ToString();
        ctx.SubscriberProfile.Add(new SubscriberProfile
        {
            Id = profileId,
            CustomerId = customerId,
            IsDeleted = false,
            OperationalStatus = SubscriberOperationalStatus.Active,
        });

        var assetId = Guid.NewGuid().ToString();
        var asset = new MsisdnAsset
        {
            Id = assetId,
            Msisdn = "0991112233",
            IsDeleted = false,
            SubscriberProfileId = profileId,
        };
        asset.TransitionTo(MsisdnPoolStatus.Active);
        ctx.MsisdnAsset.Add(asset);

        var productId = Guid.NewGuid().ToString();
        ctx.Product.Add(new Product
        {
            Id = productId,
            Name = "ميكس 500",
            Number = "MIX500",
            IsDeleted = false,
            CompatibleSubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Prepaid,
        });

        var offeringId = Guid.NewGuid().ToString();
        ctx.ProductOffering.Add(new ProductOffering
        {
            Id = offeringId,
            Name = "سيريتل ميكس 500",
            Code = "MIX_500",
            IsActive = true,
            ProductId = productId,
            CompatibleSubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Prepaid,
            IsDeleted = false,
        });

        ctx.TelecomSubscriptionTypeLookup.Add(new TelecomSubscriptionTypeLookup
        {
            Id = TelecomSubscriptionTypeWellKnownIds.Prepaid,
            NameAr = "دفع مسبق",
            NameEn = "Prepaid",
            Code = "PREPAID",
            IsDeleted = false,
        });
        ctx.TelecomSubscriptionTypeLookup.Add(new TelecomSubscriptionTypeLookup
        {
            Id = TelecomSubscriptionTypeWellKnownIds.Postpaid,
            NameAr = "فاتورة",
            NameEn = "Postpaid",
            Code = "POSTPAID",
            IsDeleted = false,
        });

        ctx.TelecomSubscription.Add(new TelecomSubscription
        {
            Id = Guid.NewGuid().ToString(),
            SubscriberProfileId = profileId,
            MsisdnAssetId = assetId,
            SubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Prepaid,
            ProductId = productId,
            IsPrimaryLine = true,
            IsDeleted = false,
        });

        ctx.Customer.Add(customer);
        await ctx.SaveChangesAsync();

        return (profileId, assetId, offeringId, productId);
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
