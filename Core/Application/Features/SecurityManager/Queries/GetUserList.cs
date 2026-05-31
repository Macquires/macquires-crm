using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Queries;



public class GetUserListResult
{
    public List<GetUserListResultDto>? Data { get; init; }
}

public class GetUserListRequest : IRequest<GetUserListResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.AdminUsersManage;
    /// <summary>Current operator (for hierarchy scope). Set by API from JWT.</summary>
    public string? ActorUserId { get; init; }
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

    public GetUserListHandler(ISecurityService securityService) => _securityService = securityService;

    public async Task<GetUserListResult> Handle(GetUserListRequest request, CancellationToken cancellationToken)
    {
        var result = await _securityService.GetUserListAsync(request.ActorUserId, cancellationToken);

        return new GetUserListResult
        {
            Data = result
        };
    }
}


