using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public sealed class IntegrationLogListItemDto
{
    public string Id { get; init; } = null!;
    public string? Msisdn { get; init; }
    public string IntegrationSystem { get; init; } = null!;
    public string OperationName { get; init; } = null!;
    public string? RequestPayload { get; init; }
    public string? ResponsePayload { get; init; }
    public long ExecutionTimeMs { get; init; }
    public bool IsSuccess { get; init; }
    public string? ResponseStatusCode { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

public class GetIntegrationLogListResult
{
    public IReadOnlyList<IntegrationLogListItemDto> Data { get; init; } = [];
    public int TotalCount { get; init; }
}

public class GetIntegrationLogListRequest : IRequest<GetIntegrationLogListResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.AdminIntegrationMonitor;
    public string? Msisdn { get; init; }
    public TelecomIntegrationSystem? IntegrationSystem { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 100;
}

public class GetIntegrationLogListValidator : AbstractValidator<GetIntegrationLogListRequest>
{
    public GetIntegrationLogListValidator()
    {
        RuleFor(x => x.Take).InclusiveBetween(1, 500);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
    }
}

public class GetIntegrationLogListHandler : IRequestHandler<GetIntegrationLogListRequest, GetIntegrationLogListResult>
{
    private readonly IQueryContext _context;

    public GetIntegrationLogListHandler(IQueryContext context) => _context = context;

    public async Task<GetIntegrationLogListResult> Handle(
        GetIntegrationLogListRequest request,
        CancellationToken cancellationToken)
    {
        var q = _context.TelecomIntegrationLog.AsNoTracking().IsDeletedEqualTo();

        if (!string.IsNullOrWhiteSpace(request.Msisdn))
        {
            var msisdn = request.Msisdn.Trim();
            q = q.Where(x => x.Msisdn != null && x.Msisdn.Contains(msisdn));
        }

        if (request.IntegrationSystem.HasValue)
        {
            q = q.Where(x => x.IntegrationSystem == request.IntegrationSystem.Value);
        }

        if (request.FromUtc.HasValue)
        {
            q = q.Where(x => x.OccurredAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            q = q.Where(x => x.OccurredAtUtc <= request.ToUtc.Value);
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(x => new IntegrationLogListItemDto
            {
                Id = x.Id,
                Msisdn = x.Msisdn,
                IntegrationSystem = x.IntegrationSystem.ToString(),
                OperationName = x.OperationName,
                RequestPayload = x.RequestPayload,
                ResponsePayload = x.ResponsePayload,
                ExecutionTimeMs = x.ExecutionTimeMs,
                IsSuccess = x.IsSuccess,
                ResponseStatusCode = x.ResponseStatusCode,
                OccurredAtUtc = x.OccurredAtUtc,
            })
            .ToListAsync(cancellationToken);

        return new GetIntegrationLogListResult { Data = rows, TotalCount = total };
    }
}
