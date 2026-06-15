using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom.Reconnect;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetReconnectEligibilityDto(
    bool Allowed,
    string MessageAr,
    string ValidationCode,
    bool RequiresBackOfficeApproval,
    string? CustomerId,
    string? Msisdn,
    string? MsisdnAssetId,
    string? SourceSuspensionOperationId,
    string? LastSuspensionType);

public class GetReconnectEligibilityResult
{
    public GetReconnectEligibilityDto? Data { get; init; }
}

public class GetReconnectEligibilityRequest : IRequest<GetReconnectEligibilityResult>, IRequireAnyPermission
{
    public string SubscriberProfileId { get; init; } = null!;
    public string MsisdnAssetId { get; init; } = null!;
    public string? ReconnectReason { get; init; }
    public string? ClearanceType { get; init; }
    public string? PaymentReference { get; init; }
    public bool FraudClearanceConfirmed { get; init; }
    public string? SourceSuspensionOperationId { get; init; }
    public IReadOnlyList<string> PermissionKeys => TelecomEligibilityPermissionSets.ReconnectAny;
}

public class GetReconnectEligibilityHandler : IRequestHandler<GetReconnectEligibilityRequest, GetReconnectEligibilityResult>
{
    private readonly IReconnectEligibilityChecker _checker;
    private readonly IQueryContext _query;

    public GetReconnectEligibilityHandler(IReconnectEligibilityChecker checker, IQueryContext query)
    {
        _checker = checker;
        _query = query;
    }

    public async Task<GetReconnectEligibilityResult> Handle(
        GetReconnectEligibilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _checker.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId,
            request.ReconnectReason ?? "CustomerRequest",
            request.ClearanceType ?? ReconnectWellKnown.Customer,
            request.PaymentReference,
            request.FraudClearanceConfirmed,
            request.SourceSuspensionOperationId,
            cancellationToken: cancellationToken);

        string? lastSuspensionType = null;
        if (!string.IsNullOrEmpty(result.SourceSuspensionOperationId))
        {
            lastSuspensionType = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
                .Where(o => o.Id == result.SourceSuspensionOperationId)
                .Select(o => o.SuspensionType)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new GetReconnectEligibilityResult
        {
            Data = new GetReconnectEligibilityDto(
                result.Allowed,
                result.MessageAr,
                result.ValidationCode,
                result.RequiresBackOfficeApproval,
                result.CustomerId,
                result.Msisdn,
                result.MsisdnAssetId,
                result.SourceSuspensionOperationId,
                lastSuspensionType),
        };
    }
}
