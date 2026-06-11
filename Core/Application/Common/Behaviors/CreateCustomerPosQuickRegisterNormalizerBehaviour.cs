using Application.Common.CQS.Queries;
using Application.Features.CustomerManager.Commands;
using MediatR;

namespace Application.Common.Behaviors;

/// <summary>Fills POS quick-register defaults before FluentValidation runs.</summary>
public sealed class CreateCustomerPosQuickRegisterNormalizerBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IQueryContext _query;

    public CreateCustomerPosQuickRegisterNormalizerBehaviour(IQueryContext query)
    {
        _query = query;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is CreateCustomerRequest create && create.PosQuickRegister)
        {
            await CreateCustomerPosQuickRegisterNormalizer.ApplyAsync(create, _query, cancellationToken);
        }

        return await next();
    }
}
