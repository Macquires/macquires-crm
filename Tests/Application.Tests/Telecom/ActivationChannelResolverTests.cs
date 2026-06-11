using Application.Common.Telecom.SellingLine;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class ActivationChannelResolverTests
{
    [Fact]
    public void Resolve_uses_Digital_when_gateway_flag_set()
    {
        var result = ActivationChannelResolver.Resolve(ActivationChannel.Showroom, isDigitalGateway: true);
        Assert.Equal(ActivationChannel.Digital, result);
    }

    [Theory]
    [InlineData(ActivationChannel.Showroom)]
    [InlineData(ActivationChannel.Dealer)]
    public void Resolve_honors_requested_channel_when_not_digital_gateway(ActivationChannel requested)
    {
        var result = ActivationChannelResolver.Resolve(requested, isDigitalGateway: false);
        Assert.Equal(requested, result);
    }

    [Fact]
    public void Resolve_defaults_to_Showroom_when_missing()
    {
        var result = ActivationChannelResolver.Resolve(null, isDigitalGateway: false);
        Assert.Equal(ActivationChannel.Showroom, result);
    }
}
