using Application.Common.Integrations;
using Domain.Enums;
using Infrastructure.TelecomIntegrations;
using Xunit;

namespace Application.Tests.Telecom;

public class HlrTerminationProvisioningTests
{
    [Fact]
    public void OperationNameFor_Termination_uses_HlrDeactivateSubscriber()
    {
        var name = HlrNetworkProvisioningService.OperationNameFor(TelecomOperationKind.Termination);
        Assert.Equal(TelecomBssOperations.HlrDeactivateSubscriber, name);
    }
}
