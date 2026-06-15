using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;
namespace Application.Features.SecurityManager.Commands;


public class UpdateUserResult
{
    public UpdateUserResultDto? Data { get; set; }
}

public class UpdateUserRequest : IRequest<UpdateUserResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => AdminPermissionSets.UsersManageAny;
    public string? UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool? EmailConfirmed { get; init; }
    public bool? IsBlocked { get; init; }
    public bool? IsDeleted { get; init; }
    public string? PrimaryMenuPersona { get; init; }
    public string? ManagerUserId { get; init; }
    public string? OrgUnitId { get; init; }
    public bool? SyncTelecomRoleFromPersona { get; init; }
}

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}

public class UpdateUserHandler : IRequestHandler<UpdateUserRequest, UpdateUserResult>
{
    private readonly ISecurityService _securityService;
    private readonly IUserAuditService _audit;
    private readonly IOperatorContext _operator;

    public UpdateUserHandler(
        ISecurityService securityService,
        IUserAuditService audit,
        IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _audit = audit;
        _operator = operatorContext;
    }

    public async Task<UpdateUserResult> Handle(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        TelecomMenuPersona? persona = null;
        if (!string.IsNullOrWhiteSpace(request.PrimaryMenuPersona)
            && Enum.TryParse<TelecomMenuPersona>(request.PrimaryMenuPersona, true, out var parsed))
        {
            persona = parsed;
        }

        var actorUserId = OperatorActor.RequireUserId(_operator);

        var result = await _securityService.UpdateUserAsync(
            request.UserId ?? "",
            request.FirstName ?? "",
            request.LastName ?? "",
            request.EmailConfirmed ?? true,
            request.IsBlocked ?? false,
            request.IsDeleted ?? false,
            actorUserId,
            persona,
            request.ManagerUserId,
            request.OrgUnitId,
            request.SyncTelecomRoleFromPersona ?? true,
            cancellationToken
            );

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId,
                UserId = request.UserId,
                ActionType = UserAuditActionTypes.UserUpdated,
                EntityType = "ApplicationUser",
                EntityId = request.UserId,
                SummaryAr = "تحديث بيانات مستخدم",
                Payload = new { request.PrimaryMenuPersona, request.IsBlocked, request.ManagerUserId },
            },
            cancellationToken);

        return new UpdateUserResult { Data = result };
    }
}

