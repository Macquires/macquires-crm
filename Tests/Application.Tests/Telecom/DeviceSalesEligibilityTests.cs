using Application.Common.Telecom.DeviceSales;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class DeviceSalesEligibilityTests
{
    [Fact]
    public void Matrix_Rejects_Delinquent()
    {
        var r = DeviceFinancingDecisionMatrix.Evaluate(new(600, true, false, false));
        Assert.Equal(DeviceFinancingDecision.Rejected, r.Decision);
    }

    [Fact]
    public void Matrix_Approves_GoodCredit()
    {
        var r = DeviceFinancingDecisionMatrix.Evaluate(new(700, false, false, false));
        Assert.Equal(DeviceFinancingDecision.Approved, r.Decision);
    }
}
