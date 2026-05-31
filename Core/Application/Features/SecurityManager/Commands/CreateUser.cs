using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;


public class CreateUserResult
{
    public CreateUserResultDto? Data { get; set; }
}

public class CreateUserRequest : IRequest<CreateUserResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.AdminUsersManage;
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? ConfirmPassword { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool? EmailConfirmed { get; init; }
    public bool? IsBlocked { get; init; }
    public bool? IsDeleted { get; init; }
    public string? CreatedById { get; init; }
    public string? PrimaryMenuPersona { get; init; }
    public string? ManagerUserId { get; init; }
    public string? OrgUnitId { get; init; }
    public bool? SyncTelecomRoleFromPersona { get; init; }
}

public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.ConfirmPassword).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}

public class CreateUserHandler : IRequestHandler<CreateUserRequest, CreateUserResult>
{
    private readonly ISecurityService _securityService;
    private readonly IUserAuditService _audit;

    public CreateUserHandler(ISecurityService securityService, IUserAuditService audit)
    {
        _securityService = securityService;
        _audit = audit;
    }

    public async Task<CreateUserResult> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        TelecomMenuPersona? persona = null;
        if (!string.IsNullOrWhiteSpace(request.PrimaryMenuPersona)
            && Enum.TryParse<TelecomMenuPersona>(request.PrimaryMenuPersona, true, out var parsed))
        {
            persona = parsed;
        }

        var result = await _securityService.CreateUserAsync(
            request.Email ?? "",
            request.Password ?? "",
            request.ConfirmPassword ?? "",
            request.FirstName ?? "",
            request.LastName ?? "",
            request.EmailConfirmed ?? true,
            request.IsBlocked ?? false,
            request.IsDeleted ?? false,
            request.CreatedById ?? "",
            persona,
            request.ManagerUserId,
            request.OrgUnitId,
            request.SyncTelecomRoleFromPersona ?? true,
            cancellationToken
            );

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.CreatedById ?? result?.UserId ?? "system",
                UserId = result?.UserId,
                ActionType = UserAuditActionTypes.UserCreated,
                EntityType = "ApplicationUser",
                EntityId = result?.UserId,
                SummaryAr = $"إنشاء مستخدم: {request.Email}",
                Payload = new { request.Email, request.PrimaryMenuPersona },
            },
            cancellationToken);

        return new CreateUserResult { Data = result };
    }
}
