using Application.Common.Telecom.SellingLine;
using Application.Features.TelecomManager.Commands;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>Normalizes activation channel (Digital gateway / Showroom default) before FluentValidation.</summary>
public sealed class CreateTelecomOperationRequestActivationChannelNormalizerBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IActivationChannelContext _channelContext;

    public CreateTelecomOperationRequestActivationChannelNormalizerBehaviour(IActivationChannelContext channelContext)
    {
        _channelContext = channelContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is CreateTelecomOperationRequest create)
        {
            ActivationChannelRequestNormalizer.Apply(create, _channelContext);
        }

        return await next();
    }
}
