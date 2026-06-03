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
}
