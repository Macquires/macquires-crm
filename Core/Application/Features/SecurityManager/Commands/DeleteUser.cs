using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;


public class DeleteUserResult
{
    public DeleteUserResultDto? Data { get; set; }
}

public class DeleteUserRequest : IRequest<DeleteUserResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => AdminPermissionSets.UsersManageAny;
    public string? UserId { get; init; }
}

public class DeleteUserValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class DeleteUserHandler : IRequestHandler<DeleteUserRequest, DeleteUserResult>
{
    private readonly ISecurityService _securityService;
    private readonly IOperatorContext _operator;

    public DeleteUserHandler(
        ISecurityService securityService,
        IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _operator = operatorContext;
    }

    public async Task<DeleteUserResult> Handle(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _securityService.DeleteUserAsync(
            request.UserId ?? "",
            OperatorActor.RequireUserId(_operator),
            cancellationToken
            );

        return new DeleteUserResult
        {
            Data = result
        };
    }
}


