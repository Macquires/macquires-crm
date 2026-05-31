using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Xunit;

namespace Domain.Tests;

public class DomainGateTests
{
    [Fact]
    public void MsisdnAsset_TransitionTo_allows_valid_path()
    {
        var asset = new MsisdnAsset { Msisdn = "0931234567" };
        asset.TransitionTo(MsisdnPoolStatus.Reserved);
        asset.TransitionTo(MsisdnPoolStatus.Active);
        Assert.Equal(MsisdnPoolStatus.Active, asset.PoolStatus);
    }

    [Fact]
    public void MsisdnAsset_TransitionTo_rejects_invalid_path()
    {
        var asset = new MsisdnAsset { Msisdn = "0931234568" };
        Assert.Throws<InvalidOperationException>(() => asset.TransitionTo(MsisdnPoolStatus.Quarantined));
    }

    [Fact]
    public void SimInventory_TransitionTo_quarantine_sets_end_date()
    {
        var sim = SimInventory.Create("8935303123456789012");
        sim.TransitionTo(SimStatus.Reserved);
        sim.TransitionTo(SimStatus.Active);
        sim.TransitionTo(SimStatus.Quarantined);
        Assert.NotNull(sim.QuarantineEndsUtc);
    }

    [Fact]
    public void IndividualCustomer_factory_sets_kind()
    {
        var c = IndividualCustomer.Create(
            "Test",
            "CST-1",
            "0101234567",
            PostalAddress.Empty,
            null,
            null,
            null,
            null);
        Assert.Equal(CustomerKind.Individual, c.CustomerKind);
        Assert.Equal("0101234567", c.NationalId);
    }

    [Fact]
    public void IndividualCustomers_with_different_national_ids_are_distinct()
    {
        var a = IndividualCustomer.Create("A", "CST-A", "1111111111", PostalAddress.Empty, null, null, null, null);
        var b = IndividualCustomer.Create("B", "CST-B", "2222222222", PostalAddress.Empty, null, null, null, null);
        Assert.NotEqual(a.NationalId, b.NationalId);
    }

    [Fact]
    public void IndividualCustomer_UpdateIdentity_changes_national_id()
    {
        var c = IndividualCustomer.Create("Test", "CST-1", "1111111111", PostalAddress.Empty, null, null, null, null);
        c.UpdateIdentity("9999999999", null, null, Gender.Unknown, null);
        Assert.Equal("9999999999", c.NationalId);
    }

    [Fact]
    public void CorporateCustomer_factory_sets_commercial_registry()
    {
        var c = CorporateCustomer.Create(
            "Corp",
            "CST-CORP",
            "CR-12345",
            PostalAddress.Empty,
            null,
            null,
            null,
            null);
        Assert.Equal(CustomerKind.Corporate, c.CustomerKind);
        Assert.Equal("CR-12345", c.CommercialRegistryNumber);
    }

    [Fact]
    public void CorporateCustomers_with_different_registries_are_distinct()
    {
        var a = CorporateCustomer.Create("A", "CST-A", "CR-11111", PostalAddress.Empty, null, null, null, null);
        var b = CorporateCustomer.Create("B", "CST-B", "CR-22222", PostalAddress.Empty, null, null, null, null);
        Assert.NotEqual(a.CommercialRegistryNumber, b.CommercialRegistryNumber);
    }
}
