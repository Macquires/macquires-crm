using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Domain.ValueObjects;
using Xunit;

namespace Domain.Tests;

public class DomainDeepDiveTests
{
    [Fact]
    public void MsisdnAsset_ReserveForCustomer_sets_24h_window()
    {
        var utc = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        var asset = new MsisdnAsset { Msisdn = "0931111111" };
        asset.ReserveForCustomer("cust-1", utc);
        Assert.Equal(MsisdnPoolStatus.Reserved, asset.PoolStatus);
        Assert.Equal("cust-1", asset.ReservedForCustomerId);
        Assert.Equal(utc.AddHours(24), asset.ReservedUntilUtc);
    }

    [Fact]
    public void MsisdnAsset_ReleaseReservationIfExpired_returns_to_available()
    {
        var utc = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        var asset = new MsisdnAsset { Msisdn = "0932222222" };
        asset.ReserveForCustomer("cust-1", utc);
        asset.ReleaseReservationIfExpired(utc.AddHours(25));
        Assert.Equal(MsisdnPoolStatus.Available, asset.PoolStatus);
        Assert.Null(asset.ReservedForCustomerId);
    }

    [Fact]
    public void SubscriptionBindingService_rejects_unavailable_msisdn()
    {
        var svc = new SubscriptionBindingService();
        var customer = IndividualCustomer.Create("A", "ACC", "1111111111", PostalAddress.Empty, null, null, null, null);
        var msisdn = new MsisdnAsset { Msisdn = "0933333333", PoolStatus = MsisdnPoolStatus.Active };
        var sim = SimInventory.Create("8935303123456789012");
        var profile = new SubscriberProfile { CustomerId = customer.Id };

        var cmd = new BindSubscriptionCommand(customer.Id, profile.Id, msisdn.Id, sim.Id, "off-1", "op-1");
        var ctx = new SubscriptionBindingContext
        {
            Customer = customer,
            MsisdnAsset = msisdn,
            SimInventory = sim,
            SubscriberProfile = profile,
            ActiveLineCountForCustomer = 0,
            MaxLinesAllowed = 10
        };

        var ex = Assert.Throws<TelecomBindingRuleException>(() => svc.Bind(cmd, ctx));
        Assert.Equal("MsisdnNotAvailable", ex.Code);
    }

    [Fact]
    public void SubscriptionBindingService_binds_when_assets_available()
    {
        var svc = new SubscriptionBindingService();
        var customer = IndividualCustomer.Create("B", "ACC2", "2222222222", PostalAddress.Empty, null, null, null, null);
        var msisdn = new MsisdnAsset { Msisdn = "0934444444", PoolStatus = MsisdnPoolStatus.Available };
        var sim = SimInventory.Create("8935303123456789013");
        var profile = new SubscriberProfile { CustomerId = customer.Id };

        var cmd = new BindSubscriptionCommand(customer.Id, profile.Id, msisdn.Id, sim.Id, "off-1", "op-1", "corr-1");
        var result = svc.Bind(cmd, new SubscriptionBindingContext
        {
            Customer = customer,
            MsisdnAsset = msisdn,
            SimInventory = sim,
            SubscriberProfile = profile,
            ActiveLineCountForCustomer = 0,
            MaxLinesAllowed = 10
        });

        Assert.Equal(MsisdnPoolStatus.Active, msisdn.PoolStatus);
        Assert.Equal(SimStatus.Active, sim.Status);
        Assert.True(result.IsPrimaryLine);
        Assert.NotNull(result.TelecomSubscription);
    }

    [Fact]
    public void SubscriberProfile_suspend_inbound_outbound_and_deactivate()
    {
        var profile = new SubscriberProfile();
        profile.SuspendInbound();
        Assert.Equal(SubscriberOperationalStatus.SuspendedInbound, profile.OperationalStatus);
        profile.SuspendOutbound();
        Assert.Equal(SubscriberOperationalStatus.SuspendedOutbound, profile.OperationalStatus);
        profile.Deactivate();
        Assert.Equal(SubscriberOperationalStatus.Deactivated, profile.OperationalStatus);
    }
}
