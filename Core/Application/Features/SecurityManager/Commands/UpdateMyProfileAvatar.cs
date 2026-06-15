using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;

public class UpdateMyProfileAvatarResult
{
    public string? Data { get; init; }
}

public class UpdateMyProfileAvatarRequest : IRequest<UpdateMyProfileAvatarResult>, IRequireAuthenticatedOperator
{
    public string? Avatar { get; init; }
}

public class UpdateMyProfileAvatarValidator : AbstractValidator<UpdateMyProfileAvatarRequest>
{
    public UpdateMyProfileAvatarValidator()
    {
        RuleFor(x => x.Avatar).NotEmpty();
    }
}

public class UpdateMyProfileAvatarHandler : IRequestHandler<UpdateMyProfileAvatarRequest, UpdateMyProfileAvatarResult>
{
    private readonly ISecurityService _securityService;
    private readonly IOperatorContext _operatorContext;

    public UpdateMyProfileAvatarHandler(ISecurityService securityService, IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _operatorContext = operatorContext;
    }

    public async Task<UpdateMyProfileAvatarResult> Handle(UpdateMyProfileAvatarRequest request, CancellationToken cancellationToken)
    {
        var userId = OperatorActor.RequireUserId(_operatorContext);

        await _securityService.ChangeAvatarAsync(
            userId,
            request.Avatar ?? "",
            cancellationToken);

        return new UpdateMyProfileAvatarResult
        {
            Data = "Update Avatar Success",
        };
    }
}
