using Application.Common.Telecom.OfferSubscription;
using Xunit;

namespace Application.Tests.Telecom;

public class OfferSubscriptionEligibilityTests
{
    [Fact]
    public void Format_includes_VAL_11_prefix()
    {
        var msg = OfferSubscriptionWellKnown.Format("08", "العرض غير متوافق مع نوع الخط الحالي");
        Assert.StartsWith("VAL-11-08:", msg);
        Assert.Contains("العرض غير متوافق", msg);
    }
}
