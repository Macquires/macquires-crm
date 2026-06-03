using Application.Common.Telecom.Termination;
using Xunit;

namespace Application.Tests.Telecom;

public class TerminationEligibilityTests
{
    [Theory]
    [InlineData("Voluntary", false)]
    [InlineData("Fraud", true)]
    [InlineData("Regulatory", true)]
    [InlineData("Collections", true)]
    public void IsBackOfficeType_detects_secure_paths(string type, bool expected)
    {
        Assert.Equal(expected, TerminationWellKnown.IsBackOfficeType(type));
    }

    [Fact]
    public void IsKnownType_accepts_all_demo_types()
    {
        Assert.True(TerminationWellKnown.IsKnownType(TerminationWellKnown.Voluntary));
        Assert.True(TerminationWellKnown.IsKnownType(TerminationWellKnown.Fraud));
        Assert.False(TerminationWellKnown.IsKnownType("Unknown"));
    }
}
