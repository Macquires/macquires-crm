using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;

public class CloneRolePermissionsResult
{
    public CloneRolePermissionsResultDto? Data { get; init; }
}

public class CloneRolePermissionsRequest : IRequest<CloneRolePermissionsResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.AdminRolesManage;
    public string? SourceRoleName { get; init; }
    public string? NewRoleName { get; init; }
    public string? CreatedById { get; init; }
}

public class CloneRolePermissionsValidator : AbstractValidator<CloneRolePermissionsRequest>
{
    public CloneRolePermissionsValidator()
    {
        RuleFor(x => x.SourceRoleName).NotEmpty();
        RuleFor(x => x.NewRoleName).NotEmpty().MaximumLength(256);
    }
}

public class CloneRolePermissionsHandler : IRequestHandler<CloneRolePermissionsRequest, CloneRolePermissionsResult>
{
    private readonly ISecurityService _securityService;
    private readonly IUserAuditService _audit;

    public CloneRolePermissionsHandler(ISecurityService securityService, IUserAuditService audit)
    {
        _securityService = securityService;
        _audit = audit;
    }

    public async Task<CloneRolePermissionsResult> Handle(CloneRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var data = await _securityService.CloneRolePermissionsAsync(
            request.SourceRoleName ?? "",
            request.NewRoleName ?? "",
            request.CreatedById,
            cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.CreatedById ?? "system",
                ActionType = UserAuditActionTypes.RolePermissionsCloned,
                EntityType = "Role",
                EntityId = request.NewRoleName,
                SummaryAr = $"نسخ صلاحيات من {request.SourceRoleName} إلى {request.NewRoleName}",
                Payload = new { request.SourceRoleName, request.NewRoleName },
            },
            cancellationToken);

        return new CloneRolePermissionsResult { Data = data };
    }
}
