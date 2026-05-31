using Application.Common.Audit;
using Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;

public class UpdateRolePermissionsResult
{
    public string? RoleName { get; init; }
    public IReadOnlyList<string>? PermissionKeys { get; init; }
}

public class UpdateRolePermissionsRequest : IRequest<UpdateRolePermissionsResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.AdminRolesManage;
    public string? RoleName { get; init; }
    public List<string>? PermissionKeys { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateRolePermissionsValidator : AbstractValidator<UpdateRolePermissionsRequest>
{
    public UpdateRolePermissionsValidator()
    {
        RuleFor(x => x.RoleName).NotEmpty();
        RuleFor(x => x.PermissionKeys).NotNull();
    }
}

public class UpdateRolePermissionsHandler : IRequestHandler<UpdateRolePermissionsRequest, UpdateRolePermissionsResult>
{
    private readonly IPermissionEvaluator _evaluator;
    private readonly IUserAuditService _audit;

    public UpdateRolePermissionsHandler(IPermissionEvaluator evaluator, IUserAuditService audit)
    {
        _evaluator = evaluator;
        _audit = audit;
    }

    public async Task<UpdateRolePermissionsResult> Handle(UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var valid = new HashSet<string>(PermissionCatalog.All.Select(p => p.Key), StringComparer.OrdinalIgnoreCase);
        var keys = (request.PermissionKeys ?? [])
            .Where(k => valid.Contains(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        await _evaluator.UpdateRolePermissionsAsync(
            request.RoleName ?? "",
            keys,
            request.UpdatedById,
            cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.UpdatedById ?? "system",
                ActionType = UserAuditActionTypes.RolePermissionsUpdated,
                EntityType = "Role",
                EntityId = request.RoleName,
                SummaryAr = $"تحديث صلاحيات الدور: {request.RoleName}",
                Payload = new { permissionCount = keys.Count },
            },
            cancellationToken);

        return new UpdateRolePermissionsResult
        {
            RoleName = request.RoleName,
            PermissionKeys = keys,
        };
    }
}
