using Application.Common.Telecom.ChangeNumber;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class ChangeNumberEligibilityTests
{
    [Theory]
    [InlineData(MsisdnCategory.Silver, true)]
    [InlineData(MsisdnCategory.Gold, true)]
    [InlineData(MsisdnCategory.Platinum, true)]
    [InlineData(MsisdnCategory.Normal, false)]
    public void IsPremiumCategory_detects_premium_pool_numbers(MsisdnCategory category, bool expected)
    {
        Assert.Equal(expected, ChangeNumberWellKnown.IsPremiumCategory(category));
    }

    [Fact]
    public void IsPremiumFeeSatisfied_normal_number_always_true()
    {
        Assert.True(ChangeNumberWellKnown.IsPremiumFeeSatisfied(MsisdnCategory.Normal, null, null));
    }

    [Theory]
    [MemberData(nameof(PremiumFeeCases))]
    public void IsPremiumFeeSatisfied_premium_requires_fee_or_reference(
        decimal? fee,
        string? paymentRef,
        bool expected)
    {
        Assert.Equal(
            expected,
            ChangeNumberWellKnown.IsPremiumFeeSatisfied(MsisdnCategory.Gold, fee, paymentRef));
    }

    public static TheoryData<decimal?, string?, bool> PremiumFeeCases =>
        new()
        {
            { null, null, false },
            { 0m, null, false },
            { 100m, null, true },
            { null, "PAY-REF-1", true },
        };

    [Theory]
    [InlineData("PortIn", true)]
    [InlineData("portin", true)]
    [InlineData("Internal", false)]
    [InlineData(null, false)]
    public void IsPortInMode_detects_port_in(string? mode, bool expected)
    {
        Assert.Equal(expected, ChangeNumberWellKnown.IsPortInMode(mode));
    }

    [Theory]
    [InlineData("MTN", true)]
    [InlineData("AFRICELL", true)]
    [InlineData("OTHER", true)]
    [InlineData("SYRIATEL", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidDonorOperator_rejects_home_operator(string? code, bool expected)
    {
        Assert.Equal(expected, ChangeNumberWellKnown.IsValidDonorOperator(code));
    }
}
