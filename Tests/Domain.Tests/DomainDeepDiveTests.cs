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
    public void MsisdnAsset_ReleaseReservation_returns_to_available_immediately()
    {
        var utc = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        var asset = new MsisdnAsset { Msisdn = "0932222223" };
        asset.ReserveForCustomer("cust-1", utc);
        asset.ReleaseReservation();
        Assert.Equal(MsisdnPoolStatus.Available, asset.PoolStatus);
        Assert.Null(asset.ReservedForCustomerId);
        Assert.Null(asset.ReservedUntilUtc);
    }

    [Fact]
    public void MsisdnAsset_RollbackFailedActivationBinding_from_active_returns_to_available()
    {
        var utc = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        var asset = new MsisdnAsset { Msisdn = "0932222224", PoolStatus = MsisdnPoolStatus.Active, SubscriberProfileId = "prof-1" };

        asset.RollbackFailedActivationBinding(utc);

        Assert.Equal(MsisdnPoolStatus.Available, asset.PoolStatus);
        Assert.Null(asset.SubscriberProfileId);
        Assert.Null(asset.QuarantineEndsUtc);
    }

    [Fact]
    public void SimInventory_RollbackFailedActivationBinding_from_active_returns_to_available()
    {
        var utc = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        var sim = SimInventory.Create("8935303123456789015");
        sim.AssignToProfile("prof-1");
        sim.TransitionTo(SimStatus.Active);

        sim.RollbackFailedActivationBinding(utc);

        Assert.Equal(SimStatus.Available, sim.Status);
        Assert.Null(sim.SubscriberProfileId);
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
    public void SubscriptionBindingService_binds_when_msisdn_and_sim_reserved_for_customer()
    {
        var svc = new SubscriptionBindingService();
        var utcNow = DateTime.UtcNow;
        var customer = IndividualCustomer.Create("C", "ACC3", "3333333333", PostalAddress.Empty, null, null, null, null);
        var msisdn = new MsisdnAsset { Msisdn = "0935555555", PoolStatus = MsisdnPoolStatus.Reserved };
        msisdn.ReserveForCustomer(customer.Id, utcNow);
        var sim = SimInventory.Create("8935303123456789014");
        sim.TransitionTo(SimStatus.Reserved);
        var profile = new SubscriberProfile { CustomerId = customer.Id };

        var cmd = new BindSubscriptionCommand(customer.Id, profile.Id, msisdn.Id, sim.Id, "off-1", "op-1", "corr-2");
        var result = svc.Bind(cmd, new SubscriptionBindingContext
        {
            Customer = customer,
            MsisdnAsset = msisdn,
            SimInventory = sim,
            SubscriberProfile = profile,
            ActiveLineCountForCustomer = 0,
            MaxLinesAllowed = 10,
            UtcNow = utcNow,
            RequireStrictReservation = true
        });

        Assert.Equal(MsisdnPoolStatus.Active, msisdn.PoolStatus);
        Assert.Equal(SimStatus.Active, sim.Status);
        Assert.Equal(profile.Id, sim.SubscriberProfileId);
        Assert.NotNull(result.TelecomSubscription);
    }

    [Fact]
    public void SubscriberProfile_suspend_inbound_outbound_and_deactivate()
    {
        var profile = new SubscriberProfile();
        profile.SuspendInbound("0931111111");
        Assert.Equal(SubscriberOperationalStatus.SuspendedInbound, profile.OperationalStatus);
        profile.SuspendOutbound("0931111111");
        Assert.Equal(SubscriberOperationalStatus.SuspendedOutbound, profile.OperationalStatus);
        profile.Deactivate();
        Assert.Equal(SubscriberOperationalStatus.Deactivated, profile.OperationalStatus);
    }
}
