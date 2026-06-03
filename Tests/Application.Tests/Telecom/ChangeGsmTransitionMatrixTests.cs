using Application.Common.Telecom.ChangeGsm;
using Domain.Common;
using Xunit;

namespace Application.Tests.Telecom;

public class ChangeGsmTransitionMatrixTests
{
    [Fact]
    public void IsAllowed_accepts_prepaid_to_postpaid()
    {
        Assert.True(ChangeGsmTransitionMatrix.IsAllowed(
            TelecomSubscriptionTypeWellKnownIds.Prepaid,
            TelecomSubscriptionTypeWellKnownIds.Postpaid));
    }

    [Fact]
    public void IsAllowed_rejects_same_type()
    {
        Assert.False(ChangeGsmTransitionMatrix.IsAllowed(
            TelecomSubscriptionTypeWellKnownIds.Prepaid,
            TelecomSubscriptionTypeWellKnownIds.Prepaid));
    }
}
