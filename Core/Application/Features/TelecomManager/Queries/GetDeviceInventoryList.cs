using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetDeviceInventoryListDto(
    string Id,
    string Imei,
    string Model,
    string? Sku,
    decimal ListPrice,
    string Status,
    string? BranchId,
    string? ReservedByOperationId);

public class GetDeviceInventoryListResult
{
    public List<GetDeviceInventoryListDto>? Data { get; init; }
}

public class GetDeviceInventoryListRequest : IRequest<GetDeviceInventoryListResult>
{
    public string? Status { get; init; }
    public string? BranchId { get; init; }
    public string? ImeiContains { get; init; }
}

public class GetDeviceInventoryListHandler : IRequestHandler<GetDeviceInventoryListRequest, GetDeviceInventoryListResult>
{
    private readonly IQueryContext _context;

    public GetDeviceInventoryListHandler(IQueryContext context) => _context = context;

    public async Task<GetDeviceInventoryListResult> Handle(
        GetDeviceInventoryListRequest request,
        CancellationToken cancellationToken)
    {
        var q = _context.DeviceInventory.AsNoTracking().IsDeletedEqualTo();

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<DeviceInventoryStatus>(request.Status, true, out var st))
        {
            q = q.Where(d => d.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(request.BranchId))
        {
            q = q.Where(d => d.BranchId == request.BranchId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.ImeiContains))
        {
            var term = request.ImeiContains.Trim();
            q = q.Where(d => d.Imei.Contains(term));
        }

        var data = await q
            .OrderBy(d => d.Model)
            .ThenBy(d => d.Imei)
            .Select(d => new GetDeviceInventoryListDto(
                d.Id,
                d.Imei,
                d.Model,
                d.Sku,
                d.ListPrice,
                d.Status.ToString(),
                d.BranchId,
                d.ReservedByOperationId))
            .ToListAsync(cancellationToken);

        return new GetDeviceInventoryListResult { Data = data };
    }
}
