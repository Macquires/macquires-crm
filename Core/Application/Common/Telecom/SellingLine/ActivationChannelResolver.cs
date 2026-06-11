using Domain.Enums;

namespace Application.Common.Telecom.SellingLine;

public static class ActivationChannelResolver
{
    public static ActivationChannel Resolve(ActivationChannel? requestedChannel, bool isDigitalGateway)
    {
        if (isDigitalGateway)
        {
            return ActivationChannel.Digital;
        }

        return requestedChannel ?? ActivationChannel.Showroom;
    }
}
