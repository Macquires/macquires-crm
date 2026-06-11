using Application.Features.TelecomManager.Commands;
using Domain.Enums;

namespace Application.Common.Telecom.SellingLine;

public static class ActivationChannelRequestNormalizer
{
    public static void Apply(CreateTelecomOperationRequest request, IActivationChannelContext channelContext)
    {
        if (request.Kind != TelecomOperationKind.NewActivation)
        {
            return;
        }

        request.ActivationChannel = ActivationChannelResolver.Resolve(
            request.ActivationChannel,
            channelContext.IsDigitalGatewayRequest);
    }
}
