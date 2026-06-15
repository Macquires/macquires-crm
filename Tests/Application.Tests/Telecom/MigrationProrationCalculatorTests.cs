using Application.Common.Telecom.OfferSubscription;
using Xunit;

namespace Application.Tests.Telecom;

public class MigrationProrationCalculatorTests
{
    [Fact]
    public void CalculateProratedAmount_returns_zero_when_price_difference_not_positive()
    {
        Assert.Equal(0m, MigrationProrationCalculator.CalculateProratedAmount(0m, 15, 30));
        Assert.Equal(0m, MigrationProrationCalculator.CalculateProratedAmount(-50m, 15, 30));
    }

    [Fact]
    public void CalculateProratedAmount_prorates_upgrade_by_remaining_days()
    {
        var amount = MigrationProrationCalculator.CalculateProratedAmount(3000m, 15, 30);
        Assert.Equal(1500m, amount);
    }

    [Fact]
    public void GetBillingCycleFraction_counts_remaining_days_in_month()
    {
        var utc = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var (remaining, inCycle) = MigrationProrationCalculator.GetBillingCycleFraction(utc);
        Assert.Equal(30, inCycle);
        Assert.Equal(16, remaining);
    }
}
