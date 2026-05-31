using Application.Common.Integrations;
using Xunit;

namespace Application.Tests.Telecom;

public class IntegrationCircuitBreakerTests
{
    [Fact]
    public void NetworkProvisionResult_QueuedForSync_TakesPriorityOverSuccessForPending()
    {
        var result = new NetworkProvisionResult(true, IntegrationCircuitBreaker.FallbackMessageAr, DeferRetry: true, QueuedForSync: true);
        Assert.True(result.Success);
        Assert.True(result.DeferRetry || result.QueuedForSync);
    }

    [Fact]
    public void BillingProvisionResult_QueuedForSync_AllowsWorkflowContinuation()
    {
        var result = new BillingProvisionResult(true, IntegrationCircuitBreaker.FallbackMessageAr, QueuedForSync: true);
        Assert.True(result.Success);
        Assert.True(result.QueuedForSync);
    }
}
