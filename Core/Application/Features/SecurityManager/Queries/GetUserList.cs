using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetUserListResult
{
    public List<GetUserListResultDto>? Data { get; init; }
}

public class GetUserListRequest : IRequest<GetUserListResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => AdminPermissionSets.UsersManageAny;
}

public class GetUserListValidator : AbstractValidator<GetUserListRequest>
{
    public GetUserListValidator()
    {
    }
}

public class GetUserListHandler : IRequestHandler<GetUserListRequest, GetUserListResult>
{
    private readonly ISecurityService _securityService;
    private readonly IOperatorContext _operator;

    public GetUserListHandler(ISecurityService securityService, IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _operator = operatorContext;
    }

    public async Task<GetUserListResult> Handle(GetUserListRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);
        var result = await _securityService.GetUserListAsync(actorUserId, cancellationToken);

        return new GetUserListResult
        {
            Data = result
        };
    }
}
