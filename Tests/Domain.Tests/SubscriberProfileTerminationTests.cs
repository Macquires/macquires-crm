using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Domain.Tests;

public class SubscriberProfileTerminationTests
{
    [Fact]
    public void Terminate_sets_operational_status()
    {
        var profile = new SubscriberProfile
        {
            CustomerId = "cust-1",
            OperationalStatus = SubscriberOperationalStatus.Active,
        };

        profile.Terminate();

        Assert.Equal(SubscriberOperationalStatus.Terminated, profile.OperationalStatus);
    }

    [Fact]
    public void Activate_after_terminate_restores_active()
    {
        var profile = new SubscriberProfile
        {
            CustomerId = "cust-1",
            OperationalStatus = SubscriberOperationalStatus.Active,
        };

        profile.Terminate();
        profile.Activate();

        Assert.Equal(SubscriberOperationalStatus.Active, profile.OperationalStatus);
    }
}
