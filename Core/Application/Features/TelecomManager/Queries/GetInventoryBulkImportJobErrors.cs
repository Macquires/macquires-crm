using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record InventoryBulkImportErrorDto(
    string Id,
    int RowNumber,
    string? Identifier,
    string? ErrorMessageAr,
    string? ErrorMessageEn);

public class GetInventoryBulkImportJobErrorsResult
{
    public List<InventoryBulkImportErrorDto> Data { get; init; } = new();
    public int TotalCount { get; init; }
}

public class GetInventoryBulkImportJobErrorsRequest : IRequest<GetInventoryBulkImportJobErrorsResult>
{
    public string JobId { get; init; } = "";
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
    public string? ActorUserId { get; init; }
}

public class GetInventoryBulkImportJobErrorsValidator : AbstractValidator<GetInventoryBulkImportJobErrorsRequest>
{
    public GetInventoryBulkImportJobErrorsValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
    }
}

public class GetInventoryBulkImportJobErrorsHandler
    : IRequestHandler<GetInventoryBulkImportJobErrorsRequest, GetInventoryBulkImportJobErrorsResult>
{
    private readonly IQueryContext _query;
    private readonly IUserScopeService _userScope;

    public GetInventoryBulkImportJobErrorsHandler(IQueryContext query, IUserScopeService userScope)
    {
        _query = query;
        _userScope = userScope;
    }

    public async Task<GetInventoryBulkImportJobErrorsResult> Handle(
        GetInventoryBulkImportJobErrorsRequest request,
        CancellationToken cancellationToken)
    {
        var job = await _query.InventoryBulkImportJob.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);
        if (job == null)
        {
            return new GetInventoryBulkImportJobErrorsResult();
        }

        if (!string.IsNullOrEmpty(request.ActorUserId)
            && !string.IsNullOrEmpty(job.CreatedById)
            && !await _userScope.CanAccessUserAsync(request.ActorUserId, job.CreatedById, cancellationToken)
            && !await _userScope.IsUnrestrictedAdminAsync(request.ActorUserId, cancellationToken))
        {
            return new GetInventoryBulkImportJobErrorsResult();
        }

        var take = Math.Clamp(request.Take, 1, 500);
        var skip = Math.Max(0, request.Skip);

        var baseQuery = _query.InventoryBulkImportError.AsNoTracking().IsDeletedEqualTo()
            .Where(e => e.JobId == request.JobId);

        var total = await baseQuery.CountAsync(cancellationToken);
        var data = await baseQuery
            .OrderBy(e => e.RowNumber)
            .Skip(skip)
            .Take(take)
            .Select(e => new InventoryBulkImportErrorDto(
                e.Id,
                e.RowNumber,
                e.Identifier,
                e.ErrorMessageAr,
                e.ErrorMessageEn))
            .ToListAsync(cancellationToken);

        return new GetInventoryBulkImportJobErrorsResult { Data = data, TotalCount = total };
    }
}
