using Application.Common.CQS.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record DeviceSaleRejectionDto(string Reason, int Count);

public class GetDeviceSaleKpisResult
{
    public int TotalVolume { get; init; }
    public int CompletedCount { get; init; }
    public int FailedCount { get; init; }
    public decimal CompletionRatePercent { get; init; }
    public decimal FalloutRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public double AvgHandlingTimeMinutes { get; init; }
    public int ManualOverrideCount { get; init; }
    public int InstallmentCount { get; init; }
    public int CashCount { get; init; }
    public List<DeviceSaleRejectionDto> RejectionReasons { get; init; } = new();
}

public class GetDeviceSaleKpisRequest : IRequest<GetDeviceSaleKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetDeviceSaleKpisHandler : IRequestHandler<GetDeviceSaleKpisRequest, GetDeviceSaleKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(30);
    private readonly IQueryContext _context;

    public GetDeviceSaleKpisHandler(IQueryContext context) => _context = context;

    public async Task<GetDeviceSaleKpisResult> Handle(
        GetDeviceSaleKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.DeviceSale
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.DeviceSaleType,
                o.DeviceFinancingNoteAr,
                o.DeviceOverrideReasonCode,
                o.ConfirmedAtUtc,
                o.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var total = ops.Count;
        var completed = ops.Count(o => o.Status == TelecomOperationStatus.Completed);
        var failed = ops.Count(o => o.Status == TelecomOperationStatus.Failed);
        var terminal = completed + failed;
        var completionRate = total > 0 ? Math.Round((decimal)completed / total * 100m, 2) : 0m;
        var falloutRate = terminal > 0 ? Math.Round((decimal)failed / terminal * 100m, 2) : 0m;

        var slaSamples = ops
            .Where(o => o.Status == TelecomOperationStatus.Completed && o.ConfirmedAtUtc.HasValue && o.CreatedAtUtc.HasValue)
            .Select(o => o.ConfirmedAtUtc!.Value - o.CreatedAtUtc!.Value)
            .ToList();
        var slaPct = slaSamples.Count == 0
            ? 100m
            : Math.Round((decimal)slaSamples.Count(d => d <= SlaTarget) / slaSamples.Count * 100m, 2);
        var avgMin = slaSamples.Count == 0 ? 0 : slaSamples.Average(d => d.TotalMinutes);

        var rejections = ops
            .Where(o => o.Status == TelecomOperationStatus.Failed && !string.IsNullOrEmpty(o.DeviceFinancingNoteAr))
            .GroupBy(o => o.DeviceFinancingNoteAr!)
            .Select(g => new DeviceSaleRejectionDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        return new GetDeviceSaleKpisResult
        {
            TotalVolume = total,
            CompletedCount = completed,
            FailedCount = failed,
            CompletionRatePercent = completionRate,
            FalloutRatePercent = falloutRate,
            SlaCompliancePercent = slaPct,
            AvgHandlingTimeMinutes = Math.Round(avgMin, 1),
            ManualOverrideCount = ops.Count(o => !string.IsNullOrEmpty(o.DeviceOverrideReasonCode)),
            InstallmentCount = ops.Count(o => o.DeviceSaleType == DeviceSaleType.Installment),
            CashCount = ops.Count(o => o.DeviceSaleType == DeviceSaleType.Cash),
            RejectionReasons = rejections,
        };
    }
}
