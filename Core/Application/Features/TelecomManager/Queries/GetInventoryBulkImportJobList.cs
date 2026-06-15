using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record InventoryBulkImportJobListItemDto(
    string Id,
    InventoryBulkImportJobStatus JobStatus,
    BulkImportJobType JobType,
    string? FileName,
    int TotalRows,
    int ProcessedRows,
    int SuccessCount,
    int ErrorCount,
    string? ErrorSummary,
    DateTime? CreatedAtUtc,
    DateTime? CompletedAtUtc,
    string? CreatedById);

public class GetInventoryBulkImportJobListResult
{
    public List<InventoryBulkImportJobListItemDto> Data { get; init; } = new();
}

public class GetInventoryBulkImportJobListRequest : IRequest<GetInventoryBulkImportJobListResult>, IRequireAnyPermission
{
    public int Take { get; init; } = 25;
    public int Skip { get; init; }
    public InventoryBulkImportJobStatus? StatusFilter { get; init; }
    public BulkImportJobType? JobTypeFilter { get; init; }
    public IReadOnlyList<string> PermissionKeys => BulkImportPermissionSets.MonitorAny;
}

public class GetInventoryBulkImportJobListHandler
    : IRequestHandler<GetInventoryBulkImportJobListRequest, GetInventoryBulkImportJobListResult>
{
    private readonly IQueryContext _query;
    private readonly IUserScopeService _userScope;
    private readonly IOperatorContext _operator;

    public GetInventoryBulkImportJobListHandler(
        IQueryContext query,
        IUserScopeService userScope,
        IOperatorContext operatorContext)
    {
        _query = query;
        _userScope = userScope;
        _operator = operatorContext;
    }

    public async Task<GetInventoryBulkImportJobListResult> Handle(
        GetInventoryBulkImportJobListRequest request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 100);
        var skip = Math.Max(0, request.Skip);
        var actorUserId = OperatorActor.RequireUserId(_operator);

        IQueryable<InventoryBulkImportJob> query = _query.InventoryBulkImportJob.AsNoTracking().IsDeletedEqualTo();

        if (!await _userScope.IsUnrestrictedAdminAsync(actorUserId, cancellationToken))
        {
            var visible = await _userScope.GetVisibleUserIdsAsync(actorUserId, cancellationToken);
            query = query.Where(j => j.CreatedById != null && visible.Contains(j.CreatedById));
        }

        if (request.StatusFilter.HasValue)
        {
            query = query.Where(j => j.JobStatus == request.StatusFilter.Value);
        }

        if (request.JobTypeFilter.HasValue)
        {
            query = query.Where(j => j.JobType == request.JobTypeFilter.Value);
        }

        var list = await query
            .OrderByDescending(j => j.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(j => new InventoryBulkImportJobListItemDto(
                j.Id,
                j.JobStatus,
                j.JobType,
                j.FileName,
                j.TotalRows,
                j.ProcessedRows,
                j.SuccessCount,
                j.ErrorCount,
                j.ErrorSummary,
                j.CreatedAtUtc,
                j.CompletedAtUtc,
                j.CreatedById))
            .ToListAsync(cancellationToken);

        return new GetInventoryBulkImportJobListResult { Data = list };
    }
}
