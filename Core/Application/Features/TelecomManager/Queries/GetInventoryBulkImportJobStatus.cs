using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class GetInventoryBulkImportJobStatusResult
{
    public InventoryBulkImportJob? Job { get; init; }
}

public class GetInventoryBulkImportJobStatusRequest : IRequest<GetInventoryBulkImportJobStatusResult>, IRequireAnyPermission
{
    public string JobId { get; init; } = "";
    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.InventoryManageAny;
}

public class GetInventoryBulkImportJobStatusHandler
    : IRequestHandler<GetInventoryBulkImportJobStatusRequest, GetInventoryBulkImportJobStatusResult>
{
    private readonly IQueryContext _query;

    public GetInventoryBulkImportJobStatusHandler(IQueryContext query) => _query = query;

    public async Task<GetInventoryBulkImportJobStatusResult> Handle(
        GetInventoryBulkImportJobStatusRequest request,
        CancellationToken cancellationToken)
    {
        var job = await _query.InventoryBulkImportJob.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);
        return new GetInventoryBulkImportJobStatusResult { Job = job };
    }
}
