namespace Application.Common.Telecom.OfferSubscription;

public static class MigrationProrationCalculator
{
    public static (int DaysRemaining, int DaysInCycle) GetBillingCycleFraction(DateTime utcNow)
    {
        var daysInCycle = DateTime.DaysInMonth(utcNow.Year, utcNow.Month);
        var today = utcNow.Date;
        var lastDayOfMonth = new DateTime(utcNow.Year, utcNow.Month, daysInCycle, 0, 0, 0, DateTimeKind.Utc);
        var daysRemaining = (lastDayOfMonth - today).Days + 1;
        if (daysRemaining < 1)
        {
            daysRemaining = 1;
        }

        return (daysRemaining, daysInCycle);
    }

    public static decimal CalculateProratedAmount(decimal priceDifference, int daysRemaining, int daysInCycle)
    {
        if (priceDifference <= 0 || daysInCycle <= 0)
        {
            return 0m;
        }

        return Math.Round(priceDifference * daysRemaining / daysInCycle, 2, MidpointRounding.AwayFromZero);
    }
}
