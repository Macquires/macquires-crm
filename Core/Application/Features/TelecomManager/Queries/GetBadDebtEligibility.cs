using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom.BadDebt;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetBadDebtEligibilityDto(
    bool Allowed,
    string MessageAr,
    string ValidationCode,
    bool RequiresBackOfficeApproval,
    string? CustomerId,
    string? Msisdn,
    string? MsisdnAssetId,
    decimal OutstandingBalanceSnapshot);

public class GetBadDebtEligibilityResult
{
    public GetBadDebtEligibilityDto? Data { get; init; }
}

public class GetBadDebtEligibilityRequest : IRequest<GetBadDebtEligibilityResult>, IRequireAnyPermission
{
    public string SubscriberProfileId { get; init; } = null!;
    public string MsisdnAssetId { get; init; } = null!;
    public string? CollectionAction { get; init; }
    public string? DunningStage { get; init; }
    public string? PaymentReference { get; init; }
    public decimal? CollectedAmount { get; init; }
    public decimal? WriteOffAmount { get; init; }
    public bool CollectionApprovalConfirmed { get; init; }
    public IReadOnlyList<string> PermissionKeys => TelecomEligibilityPermissionSets.BadDebtAny;
}

public class GetBadDebtEligibilityHandler : IRequestHandler<GetBadDebtEligibilityRequest, GetBadDebtEligibilityResult>
{
    private readonly IBadDebtEligibilityChecker _checker;
    private readonly IQueryContext _query;

    public GetBadDebtEligibilityHandler(IBadDebtEligibilityChecker checker, IQueryContext query)
    {
        _checker = checker;
        _query = query;
    }

    public async Task<GetBadDebtEligibilityResult> Handle(
        GetBadDebtEligibilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _checker.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.MsisdnAssetId,
            request.CollectionAction ?? BadDebtWellKnown.PaymentRecorded,
            request.DunningStage,
            request.PaymentReference,
            request.CollectedAmount,
            request.WriteOffAmount,
            request.CollectionApprovalConfirmed,
            cancellationToken: cancellationToken);

        return new GetBadDebtEligibilityResult
        {
            Data = new GetBadDebtEligibilityDto(
                result.Allowed,
                result.MessageAr,
                result.ValidationCode,
                result.RequiresBackOfficeApproval,
                result.CustomerId,
                result.Msisdn,
                result.MsisdnAssetId,
                result.OutstandingBalanceSnapshot),
        };
    }
}
