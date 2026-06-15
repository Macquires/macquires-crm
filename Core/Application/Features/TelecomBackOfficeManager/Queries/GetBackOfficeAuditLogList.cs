using Application.Common.Audit;
using Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomBackOfficeManager.Queries;

public class BackOfficeAuditSummaryDto
{
    public int LogsToday { get; init; }
    public int NetworkCommandsToday { get; init; }
    public int TotalMatching { get; init; }
}

public class GetBackOfficeAuditLogListResult
{
    public IReadOnlyList<UserAuditLogListItemDto>? Data { get; init; }
    public int TotalCount { get; init; }
    public BackOfficeAuditSummaryDto Summary { get; init; } = new();
}

public class GetBackOfficeAuditLogListRequest : IRequest<GetBackOfficeAuditLogListResult>, IRequireAnyPermission
{
    public string? SearchTerm { get; init; }
    public string? ActionType { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 100;
    public IReadOnlyList<string> PermissionKeys => AdminPermissionSets.AuditViewAny;
}

public class GetBackOfficeAuditLogListValidator : AbstractValidator<GetBackOfficeAuditLogListRequest>
{
    public GetBackOfficeAuditLogListValidator()
    {
        RuleFor(x => x.Take).InclusiveBetween(1, 500);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SearchTerm)
            .Must(s => string.IsNullOrWhiteSpace(s) || s.Trim().Length >= 2)
            .WithMessage("أدخل حرفين على الأقل للبحث.");
    }
}

public class GetBackOfficeAuditLogListHandler : IRequestHandler<GetBackOfficeAuditLogListRequest, GetBackOfficeAuditLogListResult>
{
    private static readonly string[] BackOfficeActionTypes =
    [
        UserAuditActionTypes.SubscriberSearched,
        UserAuditActionTypes.CustomerViewed,
        UserAuditActionTypes.NetworkCommandExecuted,
        UserAuditActionTypes.TicketResolved,
        UserAuditActionTypes.BulkImportStarted,
        UserAuditActionTypes.BulkImportExecuted,
        UserAuditActionTypes.TelecomOperationConfirmed,
        UserAuditActionTypes.BackOfficeTelecomApproved,
        UserAuditActionTypes.BackOfficeTelecomRejected,
    ];

    private readonly IUserAuditReadService _read;
    private readonly IOperatorContext _operator;

    public GetBackOfficeAuditLogListHandler(IUserAuditReadService read, IOperatorContext operatorContext)
    {
        _read = read;
        _operator = operatorContext;
    }

    public async Task<GetBackOfficeAuditLogListResult> Handle(
        GetBackOfficeAuditLogListRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);
        var listQuery = BuildListQuery(request, actorUserId);
        var result = await _read.QueryAsync(listQuery, cancellationToken);

        var todayStart = DateTime.UtcNow.Date;

        var logsToday = await _read.CountAsync(
            new UserAuditLogQuery
            {
                ActorUserId = actorUserId,
                ActionTypes = BackOfficeActionTypes,
                FromUtc = todayStart,
            },
            cancellationToken);

        var networkToday = await _read.CountAsync(
            new UserAuditLogQuery
            {
                ActorUserId = actorUserId,
                ActionType = UserAuditActionTypes.NetworkCommandExecuted,
                FromUtc = todayStart,
            },
            cancellationToken);

        return new GetBackOfficeAuditLogListResult
        {
            Data = result.Items,
            TotalCount = result.TotalCount,
            Summary = new BackOfficeAuditSummaryDto
            {
                LogsToday = logsToday,
                NetworkCommandsToday = networkToday,
                TotalMatching = result.TotalCount,
            },
        };
    }

    private static UserAuditLogQuery BuildListQuery(GetBackOfficeAuditLogListRequest request, string actorUserId)
    {
        var hasActionFilter = !string.IsNullOrWhiteSpace(request.ActionType);

        return new UserAuditLogQuery
        {
            ActorUserId = actorUserId,
            SearchTerm = request.SearchTerm,
            ActionType = hasActionFilter ? request.ActionType : null,
            ActionTypes = hasActionFilter ? null : BackOfficeActionTypes,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Skip = request.Skip,
            Take = request.Take,
        };
    }
}
