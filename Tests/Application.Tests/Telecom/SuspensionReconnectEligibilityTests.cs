using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.Suspension;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class SuspensionReconnectEligibilityTests
{
    [Fact]
    public void SuspensionWellKnown_recognizes_fraud_and_regulatory()
    {
        Assert.True(SuspensionWellKnown.IsBackOfficeType(SuspensionWellKnown.Fraud));
        Assert.True(SuspensionWellKnown.IsBackOfficeType(SuspensionWellKnown.Regulatory));
        Assert.False(SuspensionWellKnown.IsBackOfficeType(SuspensionWellKnown.CustomerRequest));
    }

    [Fact]
    public void ReconnectWellKnown_recognizes_clearance_types()
    {
        Assert.True(ReconnectWellKnown.IsKnownClearanceType(ReconnectWellKnown.Payment));
        Assert.True(ReconnectWellKnown.IsKnownClearanceType("customer"));
    }

    [Fact]
    public void TelecomOperationKind_has_suspension_and_reconnect()
    {
        Assert.Equal(8, (int)TelecomOperationKind.TemporarySuspension);
        Assert.Equal(9, (int)TelecomOperationKind.Reconnect);
    }
}
