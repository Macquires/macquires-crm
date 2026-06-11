using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Telecom;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetHlrProvisioningLogListDto
{
    public string? Id { get; init; }
    public string? TelecomOperationRequestId { get; init; }
    public string? OperationNumber { get; init; }
    public int AttemptNumber { get; init; }
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? IntegrationTarget { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetHlrProvisioningLogListResult
{
    public List<GetHlrProvisioningLogListDto>? Data { get; init; }
    public int TotalCount { get; init; }
}

public class GetHlrProvisioningLogListRequest : IRequest<GetHlrProvisioningLogListResult>
{
    public string? TelecomOperationRequestId { get; init; }
    public string? OperationNumber { get; init; }
    public bool? Success { get; init; }
    public bool IsDeleted { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 25;
}

public class GetHlrProvisioningLogListValidator : AbstractValidator<GetHlrProvisioningLogListRequest>
{
    public GetHlrProvisioningLogListValidator()
    {
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
    }
}

public class GetHlrProvisioningLogListHandler : IRequestHandler<GetHlrProvisioningLogListRequest, GetHlrProvisioningLogListResult>
{
    private readonly IQueryContext _context;

    public GetHlrProvisioningLogListHandler(IQueryContext context) => _context = context;

    public async Task<GetHlrProvisioningLogListResult> Handle(
        GetHlrProvisioningLogListRequest request,
        CancellationToken cancellationToken)
    {
        var hlrTargets = ProvisioningIntegrationLogTargets.HlrTargets;
        var query = _context.BillingIntegrationLog
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Where(x => x.IntegrationTarget != null && hlrTargets.Contains(x.IntegrationTarget));

        if (!string.IsNullOrEmpty(request.TelecomOperationRequestId))
        {
            query = query.Where(x => x.TelecomOperationRequestId == request.TelecomOperationRequestId);
        }

        if (!string.IsNullOrWhiteSpace(request.OperationNumber))
        {
            var opNum = request.OperationNumber.Trim();
            query = query.Where(x =>
                _context.TelecomOperationRequest.Any(o =>
                    o.Id == x.TelecomOperationRequestId
                    && o.Number != null
                    && o.Number.Contains(opNum)));
        }

        if (request.Success.HasValue)
        {
            query = query.Where(x => x.Success == request.Success.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        if (total == 0)
        {
            return new GetHlrProvisioningLogListResult
            {
                Data = [],
                TotalCount = 0,
            };
        }

        var logs = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        var opIds = logs.Select(x => x.TelecomOperationRequestId).Distinct().ToList();
        var opRows = await _context.TelecomOperationRequest
            .AsNoTracking()
            .Where(o => opIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Number })
            .ToListAsync(cancellationToken);
        var opNumbers = opRows.ToDictionary(x => x.Id, x => x.Number ?? string.Empty);

        var list = logs.Select(x => new GetHlrProvisioningLogListDto
        {
            Id = x.Id,
            TelecomOperationRequestId = x.TelecomOperationRequestId,
            OperationNumber = x.TelecomOperationRequestId != null && opNumbers.TryGetValue(x.TelecomOperationRequestId, out var n) ? n : string.Empty,
            AttemptNumber = x.AttemptNumber,
            Success = x.Success,
            Message = x.Message,
            IntegrationTarget = x.IntegrationTarget,
            CreatedAtUtc = x.CreatedAtUtc,
        }).ToList();

        return new GetHlrProvisioningLogListResult { Data = list, TotalCount = total };
    }
}
