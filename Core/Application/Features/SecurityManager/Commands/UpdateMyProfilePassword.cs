using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;

public class UpdateMyProfilePasswordResult
{
    public string? Data { get; init; }
}

public class UpdateMyProfilePasswordRequest : IRequest<UpdateMyProfilePasswordResult>, IRequireAuthenticatedOperator
{
    public string? OldPassword { get; init; }
    public string? NewPassword { get; init; }
    public string? ConfirmNewPassword { get; init; }
}

public class UpdateMyProfilePasswordValidator : AbstractValidator<UpdateMyProfilePasswordRequest>
{
    public UpdateMyProfilePasswordValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty();
        RuleFor(x => x.ConfirmNewPassword).NotEmpty();
    }
}

public class UpdateMyProfilePasswordHandler : IRequestHandler<UpdateMyProfilePasswordRequest, UpdateMyProfilePasswordResult>
{
    private readonly ISecurityService _securityService;
    private readonly IOperatorContext _operatorContext;

    public UpdateMyProfilePasswordHandler(ISecurityService securityService, IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _operatorContext = operatorContext;
    }

    public async Task<UpdateMyProfilePasswordResult> Handle(
        UpdateMyProfilePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = OperatorActor.RequireUserId(_operatorContext);

        await _securityService.ChangePasswordAsync(
            userId,
            request.OldPassword ?? "",
            request.NewPassword ?? "",
            request.ConfirmNewPassword ?? "",
            cancellationToken);

        return new UpdateMyProfilePasswordResult
        {
            Data = "Update Password Success",
        };
    }
}
