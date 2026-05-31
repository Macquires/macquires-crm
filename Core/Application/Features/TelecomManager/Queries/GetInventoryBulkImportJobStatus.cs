using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class GetInventoryBulkImportJobStatusResult
{
    public InventoryBulkImportJob? Job { get; init; }
}

public class GetInventoryBulkImportJobStatusRequest : IRequest<GetInventoryBulkImportJobStatusResult>
{
    public string JobId { get; init; } = "";
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
