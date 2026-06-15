using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom.ChangeGsm;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProductManager.Queries;

public record GetChangeGsmEligibleTargetDto
{
    public string? Id { get; init; }
    public string? Code { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
}

public class GetChangeGsmEligibleTargetsResult
{
    public List<GetChangeGsmEligibleTargetDto> Data { get; init; } = new();
    public string? CurrentSubscriptionTypeId { get; init; }
    public string? CurrentSubscriptionTypeCode { get; init; }
    public string? CurrentSubscriptionTypeLabel { get; init; }
    public string? CurrentMsisdn { get; init; }
}

public class GetChangeGsmEligibleTargetsRequest : IRequest<GetChangeGsmEligibleTargetsResult>, IRequireAnyPermission
{
    public string SubscriberProfileId { get; init; } = "";
    public string? MsisdnAssetId { get; init; }
    public IReadOnlyList<string> PermissionKeys => ProductCatalogPermissionSets.ReadAny;
}

public class GetChangeGsmEligibleTargetsHandler
    : IRequestHandler<GetChangeGsmEligibleTargetsRequest, GetChangeGsmEligibleTargetsResult>
{
    private readonly IQueryContext _context;

    public GetChangeGsmEligibleTargetsHandler(IQueryContext context) => _context = context;

    public async Task<GetChangeGsmEligibleTargetsResult> Handle(
        GetChangeGsmEligibleTargetsRequest request,
        CancellationToken cancellationToken)
    {
        var profileId = (request.SubscriberProfileId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(profileId))
        {
            return new GetChangeGsmEligibleTargetsResult();
        }

        var subs = await _context.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfileId == profileId)
            .Include(s => s.SubscriptionTypeLookup)
            .Include(s => s.MsisdnAsset)
            .ToListAsync(cancellationToken);

        if (subs.Count == 0)
        {
            return new GetChangeGsmEligibleTargetsResult();
        }

        var msisdnId = (request.MsisdnAssetId ?? string.Empty).Trim();
        TelecomSubscription? chosen = null;
        if (!string.IsNullOrEmpty(msisdnId))
        {
            chosen = subs.FirstOrDefault(s => s.MsisdnAssetId == msisdnId);
        }

        chosen ??= subs.FirstOrDefault(s => s.IsPrimaryLine) ?? subs[0];

        var sourceId = chosen.SubscriptionTypeId;
        var allowedIds = ChangeGsmTransitionMatrix.GetAllowedTargets(sourceId);

        var allowedSet = allowedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allTypes = await _context.TelecomSubscriptionTypeLookup.AsNoTracking().IsDeletedEqualTo()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);

        var lookups = allTypes
            .Where(t => allowedSet.Contains(t.Id))
            .Select(t => new GetChangeGsmEligibleTargetDto
            {
                Id = t.Id,
                Code = t.Code,
                NameAr = t.NameAr,
                NameEn = t.NameEn,
            })
            .ToList();

        var lookup = chosen.SubscriptionTypeLookup;
        var label = lookup != null ? $"{lookup.NameAr} ({lookup.Code})" : sourceId;

        return new GetChangeGsmEligibleTargetsResult
        {
            Data = lookups,
            CurrentSubscriptionTypeId = sourceId,
            CurrentSubscriptionTypeCode = lookup?.Code,
            CurrentSubscriptionTypeLabel = label,
            CurrentMsisdn = chosen.MsisdnAsset?.Msisdn,
        };
    }
}
