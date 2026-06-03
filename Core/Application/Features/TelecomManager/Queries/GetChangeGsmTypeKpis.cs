using Application.Common.CQS.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record ChangeGsmReasonCountDto(string Reason, int Count);

public class GetChangeGsmTypeKpisResult
{
    public int TotalToday { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public decimal FailureRatePercent { get; init; }
    public List<ChangeGsmReasonCountDto> TopReasons { get; init; } = new();
}

public class GetChangeGsmTypeKpisRequest : IRequest<GetChangeGsmTypeKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetChangeGsmTypeKpisHandler : IRequestHandler<GetChangeGsmTypeKpisRequest, GetChangeGsmTypeKpisResult>
{
    private readonly IQueryContext _context;

    public GetChangeGsmTypeKpisHandler(IQueryContext context) => _context = context;

    public async Task<GetChangeGsmTypeKpisResult> Handle(
        GetChangeGsmTypeKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.ChangeGsmType
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new { o.Status, o.GsmMigrationReason })
            .ToListAsync(cancellationToken);

        var total = ops.Count;
        var completed = ops.Count(o => o.Status == TelecomOperationStatus.Completed);
        var failed = ops.Count(o => o.Status == TelecomOperationStatus.Failed);
        var terminal = completed + failed;
        var failRate = terminal > 0 ? Math.Round((decimal)failed / terminal * 100m, 2) : 0m;

        var reasons = ops
            .Where(o => !string.IsNullOrWhiteSpace(o.GsmMigrationReason))
            .GroupBy(o => o.GsmMigrationReason!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ChangeGsmReasonCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new GetChangeGsmTypeKpisResult
        {
            TotalToday = total,
            CompletedToday = completed,
            FailedToday = failed,
            FailureRatePercent = failRate,
            TopReasons = reasons,
        };
    }
}
