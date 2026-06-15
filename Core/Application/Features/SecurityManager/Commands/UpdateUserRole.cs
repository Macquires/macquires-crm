using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;



public class UpdateUserRoleResult
{
    public List<string>? Data { get; set; }
}

public class UpdateUserRoleRequest : IRequest<UpdateUserRoleResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => AdminPermissionSets.UsersManageAny;
    public string? UserId { get; init; }
    public string? RoleName { get; init; }
    public bool? AccessGranted { get; init; }
}

public class UpdateUserRoleValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleName).NotEmpty();
    }
}

public class UpdateUserRoleHandler : IRequestHandler<UpdateUserRoleRequest, UpdateUserRoleResult>
{
    private readonly ISecurityService _securityService;
    private readonly IUserAuditService _audit;
    private readonly IOperatorContext _operator;

    public UpdateUserRoleHandler(
        ISecurityService securityService,
        IUserAuditService audit,
        IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _audit = audit;
        _operator = operatorContext;
    }

    public async Task<UpdateUserRoleResult> Handle(UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);

        var result = await _securityService.UpdateUserRoleAsync(
            request.UserId ?? "",
            request.RoleName ?? "",
            request.AccessGranted ?? true,
            cancellationToken
            );

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId,
                UserId = request.UserId,
                ActionType = UserAuditActionTypes.UserRolesUpdated,
                EntityType = "ApplicationUser",
                EntityId = request.UserId,
                SummaryAr = $"تحديث دور: {request.RoleName} → {(request.AccessGranted == true ? "منح" : "سحب")}",
                Payload = new { request.RoleName, request.AccessGranted, roles = result },
            },
            cancellationToken);

        return new UpdateUserRoleResult { Data = result };
    }
}


