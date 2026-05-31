using Application.Common.Audit;
using Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetUserAuditLogListResult
{
    public IReadOnlyList<UserAuditLogListItemDto>? Data { get; init; }
    public int TotalCount { get; init; }
}

public class GetUserAuditLogListRequest : IRequest<GetUserAuditLogListResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.AdminAuditView;
    public string? SearchTerm { get; init; }
    public string? CustomerId { get; init; }
    public string? UserId { get; init; }
    public string? ActorUserId { get; init; }
    public string? ActionType { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 100;
}

public class GetUserAuditLogListValidator : AbstractValidator<GetUserAuditLogListRequest>
{
    public GetUserAuditLogListValidator()
    {
        RuleFor(x => x.Take).InclusiveBetween(1, 500);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SearchTerm)
            .Must(s => string.IsNullOrWhiteSpace(s) || s.Trim().Length >= 2)
            .WithMessage("أدخل حرفين على الأقل للبحث عن مشترك.");
    }
}

public class GetUserAuditLogListHandler : IRequestHandler<GetUserAuditLogListRequest, GetUserAuditLogListResult>
{
    private readonly IUserAuditReadService _read;

    public GetUserAuditLogListHandler(IUserAuditReadService read) => _read = read;

    public async Task<GetUserAuditLogListResult> Handle(GetUserAuditLogListRequest request, CancellationToken cancellationToken)
    {
        var result = await _read.QueryAsync(
            new UserAuditLogQuery
            {
                SearchTerm = request.SearchTerm,
                CustomerId = request.CustomerId,
                UserId = request.UserId,
                ActorUserId = request.ActorUserId,
                ActionType = request.ActionType,
                FromUtc = request.FromUtc,
                ToUtc = request.ToUtc,
                Skip = request.Skip,
                Take = request.Take,
            },
            cancellationToken);

        return new GetUserAuditLogListResult
        {
            Data = result.Items,
            TotalCount = result.TotalCount,
        };
    }
}
