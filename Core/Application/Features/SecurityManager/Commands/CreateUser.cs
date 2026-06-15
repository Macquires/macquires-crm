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

public class CreateUserRequest : IRequest<CreateUserResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => AdminPermissionSets.UsersManageAny;
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? ConfirmPassword { get; init; }
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

public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[\^$*.\[\]{}()?\-""!@#%&/\\,><':;|_~`]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(128);
    }
}

public class CreateUserHandler : IRequestHandler<CreateUserRequest, CreateUserResult>
{
    private readonly ISecurityService _securityService;
    private readonly IUserAuditService _audit;
    private readonly IOperatorContext _operator;

    public CreateUserHandler(
        ISecurityService securityService,
        IUserAuditService audit,
        IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _audit = audit;
        _operator = operatorContext;
    }

    public async Task<CreateUserResult> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        TelecomMenuPersona? persona = null;
        if (!string.IsNullOrWhiteSpace(request.PrimaryMenuPersona)
            && Enum.TryParse<TelecomMenuPersona>(request.PrimaryMenuPersona, true, out var parsed))
        {
            persona = parsed;
        }

        var actorUserId = OperatorActor.RequireUserId(_operator);

        var result = await _securityService.CreateUserAsync(
            request.Email ?? "",
            request.Password ?? "",
            request.ConfirmPassword ?? "",
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
