using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public sealed class SupervisorInterventionRowDto
{
    public string OperationId { get; init; } = null!;
    public string OperationNumber { get; init; } = null!;
    public TelecomOperationKind Kind { get; init; }
    public string? BranchId { get; init; }
    public string? BranchName { get; init; }
    public string InterventionType { get; init; } = null!;
    public string? OverrideReasonCode { get; init; }
    public string? ApprovalLevelRequired { get; init; }
    public string? BarringLevel { get; init; }
    public TelecomOperationStatus Status { get; init; }
    public DateTime? ConfirmedAtUtc { get; init; }
    public bool SupervisorApproved { get; init; }
}

public class GetSupervisorInterventionAuditResult
{
    public int TotalInterventions { get; init; }
    public int BypassCount { get; init; }
    public int ApprovedCount { get; init; }
    public IReadOnlyList<SupervisorInterventionRowDto> Rows { get; init; } = [];
    public string ScopeLabelAr { get; init; } = null!;
}

public record GetSupervisorInterventionAuditRequest(
    string? RegionId,
    string? BranchId,
    DateTime? FromUtc,
    DateTime? ToUtc)
    : IRequest<GetSupervisorInterventionAuditResult>, IOperationalKpiRequest;

public class GetSupervisorInterventionAuditHandler
    : IRequestHandler<GetSupervisorInterventionAuditRequest, GetSupervisorInterventionAuditResult>
{
    private readonly IQueryContext _context;
    private readonly IOperationalAnalyticsScopeService _scopeService;

    public GetSupervisorInterventionAuditHandler(
        IQueryContext context,
        IOperationalAnalyticsScopeService scopeService)
    {
        _context = context;
        _scopeService = scopeService;
    }

    public async Task<GetSupervisorInterventionAuditResult> Handle(
        GetSupervisorInterventionAuditRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        if (scope.EffectiveBranchIds.Count == 0)
        {
            return new GetSupervisorInterventionAuditResult { ScopeLabelAr = scope.ScopeLabelAr };
        }

        var from = request.FromUtc ?? DateTime.UtcNow.AddDays(-30);
        var to = request.ToUtc ?? DateTime.UtcNow;
        var branchIds = scope.EffectiveBranchIds;

        var branchNames = await _context.OrgUnit.AsNoTracking()
            .Where(o => branchIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.NameAr, cancellationToken);

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                && o.CreatedAtUtc >= from
                && o.CreatedAtUtc <= to
                && o.BranchId != null
                && branchIds.Contains(o.BranchId)
                && (!string.IsNullOrEmpty(o.OverrideReasonCode)
                    || !string.IsNullOrEmpty(o.BarringLevel)
                    || o.ApprovalLevelRequired == "BackOffice"))
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(200)
            .Select(o => new
            {
                o.Id,
                o.Number,
                o.Kind,
                o.BranchId,
                o.OverrideReasonCode,
                o.ApprovalLevelRequired,
                o.BarringLevel,
                o.Status,
                o.ConfirmedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var rows = ops.Select(o =>
        {
            var bypass = !string.IsNullOrEmpty(o.OverrideReasonCode);
            var approved = string.Equals(o.ApprovalLevelRequired, "BackOffice", StringComparison.OrdinalIgnoreCase)
                && o.Status is TelecomOperationStatus.Completed or TelecomOperationStatus.Confirmed;
            var interventionType = bypass
                ? "تجاوز مشرف / Override"
                : !string.IsNullOrEmpty(o.BarringLevel)
                    ? $"حظر: {o.BarringLevel}"
                    : "اعتماد باك أوفيس";

            return new SupervisorInterventionRowDto
            {
                OperationId = o.Id,
                OperationNumber = o.Number,
                Kind = o.Kind,
                BranchId = o.BranchId,
                BranchName = o.BranchId != null ? branchNames.GetValueOrDefault(o.BranchId) : null,
                InterventionType = interventionType,
                OverrideReasonCode = o.OverrideReasonCode,
                ApprovalLevelRequired = o.ApprovalLevelRequired,
                BarringLevel = o.BarringLevel,
                Status = o.Status,
                ConfirmedAtUtc = o.ConfirmedAtUtc,
                SupervisorApproved = approved && !bypass,
            };
        }).ToList();

        return new GetSupervisorInterventionAuditResult
        {
            TotalInterventions = rows.Count,
            BypassCount = rows.Count(r => !string.IsNullOrEmpty(r.OverrideReasonCode)),
            ApprovedCount = rows.Count(r => r.SupervisorApproved),
            Rows = rows,
            ScopeLabelAr = scope.ScopeLabelAr,
        };
    }
}
