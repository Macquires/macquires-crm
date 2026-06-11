using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Telecom;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetMsisdnAssetPoolListDto
{
    public string? Id { get; init; }
    public string? Msisdn { get; init; }
    public string? Iccid { get; init; }
    public string? Imsi { get; init; }
    public MsisdnPoolStatus PoolStatus { get; init; }
    public string? PoolStatusName { get; init; }
    public DateTime? QuarantineEndsUtc { get; init; }
    public DateTime? ReservedUntilUtc { get; init; }
    public string? SubscriberProfileId { get; init; }
    public string? SubscriberName { get; init; }
    public string? ProductName { get; init; }
    public string? CompatibleSubscriptionTypeId { get; init; }
    public string? CompatibleSubscriptionTypeCode { get; init; }
    public string? CompatibleSubscriptionTypeNameAr { get; init; }
    public string? CompatibleSubscriptionTypeNameEn { get; init; }
    public MsisdnCategory Category { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetMsisdnAssetPoolListProfile : Profile
{
    public GetMsisdnAssetPoolListProfile()
    {
        CreateMap<MsisdnAsset, GetMsisdnAssetPoolListDto>()
            .ForMember(d => d.PoolStatusName, o => o.MapFrom(s => s.PoolStatus.ToString()))
            .ForMember(d => d.SubscriberName, o => o.MapFrom(s => s.SubscriberProfile != null && s.SubscriberProfile.Customer != null
                ? s.SubscriberProfile.Customer.DisplayName
                : string.Empty))
            .ForMember(d => d.Iccid, o => o.Ignore())
            .ForMember(d => d.Imsi, o => o.Ignore())
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
            .ForMember(d => d.CompatibleSubscriptionTypeId, o => o.Ignore())
            .ForMember(d => d.CompatibleSubscriptionTypeCode, o => o.Ignore())
            .ForMember(d => d.CompatibleSubscriptionTypeNameAr, o => o.Ignore())
            .ForMember(d => d.CompatibleSubscriptionTypeNameEn, o => o.Ignore());
    }

    internal static SimInventory? ResolveLinkedSim(MsisdnAsset asset) =>
        MsisdnAssetKitResolver.ResolveLinkedSim(asset.SubscriberProfile?.SimInventories);
}

public class GetMsisdnAssetPoolListResult
{
    public List<GetMsisdnAssetPoolListDto>? Data { get; init; }
}

/// <summary>Read-only pool list; authorization is enforced at the API controller (telecom roles).</summary>
public class GetMsisdnAssetPoolListRequest : IRequest<GetMsisdnAssetPoolListResult>
{
    public bool IsDeleted { get; init; }
    public string? Status { get; init; }
    public string? SubscriptionTypeId { get; init; }
}

public class GetMsisdnAssetPoolListHandler : IRequestHandler<GetMsisdnAssetPoolListRequest, GetMsisdnAssetPoolListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetMsisdnAssetPoolListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetMsisdnAssetPoolListResult> Handle(
        GetMsisdnAssetPoolListRequest request,
        CancellationToken cancellationToken)
    {
        var query = _context.MsisdnAsset
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.SubscriberProfile!)
                .ThenInclude(p => p.Customer)
            .Include(x => x.SubscriberProfile!)
                .ThenInclude(p => p.SimInventories)
            .Include(x => x.IntendedSubscriptionTypeLookup)
            .Include(x => x.Product!)
                .ThenInclude(p => p.CompatibleSubscriptionTypeLookup)
            .AsQueryable();

        var subscriptionTypeId = (request.SubscriptionTypeId ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(subscriptionTypeId))
        {
            query = query.Where(x =>
                x.IntendedSubscriptionTypeId == subscriptionTypeId
                || (x.IntendedSubscriptionTypeId == null
                    && x.Product != null
                    && x.Product.CompatibleSubscriptionTypeId == subscriptionTypeId));
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            var poolStatus = ResolvePoolStatusFilter(request.Status);
            if (poolStatus.HasValue)
            {
                query = query.Where(x => x.PoolStatus == poolStatus.Value);
            }
        }

        var list = await query.OrderBy(x => x.Msisdn).ToListAsync(cancellationToken);
        var assetIds = list.Select(x => x.Id).ToList();

        var operationSimByAsset = await (
            from op in _context.TelecomOperationRequest.AsNoTracking()
            where op.MsisdnAssetId != null
                  && op.SimInventoryId != null
                  && assetIds.Contains(op.MsisdnAssetId)
            orderby op.CreatedAtUtc descending
            select new { op.MsisdnAssetId, op.SimInventoryId })
            .GroupBy(x => x.MsisdnAssetId!)
            .ToDictionaryAsync(g => g.Key, g => g.First().SimInventoryId!, cancellationToken);

        var operationSimIds = operationSimByAsset.Values.Distinct().ToList();
        var operationSims = operationSimIds.Count == 0
            ? new Dictionary<string, SimInventory>()
            : await _context.SimInventory.AsNoTracking()
                .Where(s => operationSimIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);

        var iccidCandidates = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asset in list)
        {
            if (!MsisdnAssetKitResolver.ShouldExposePoolPairing(asset.PoolStatus))
            {
                continue;
            }

            foreach (var candidate in MsisdnAssetKitResolver.CollectIccidCandidates(asset))
            {
                iccidCandidates.Add(candidate);
            }
        }

        var simsByIccid = iccidCandidates.Count == 0
            ? new Dictionary<string, SimInventory>(StringComparer.Ordinal)
            : await _context.SimInventory.AsNoTracking()
                .Where(s => iccidCandidates.Contains(s.Iccid))
                .ToDictionaryAsync(s => s.Iccid, StringComparer.Ordinal, cancellationToken);

        var data = list.Select(asset =>
        {
            SimInventory? operationSim = null;
            if (operationSimByAsset.TryGetValue(asset.Id, out var simId)
                && operationSims.TryGetValue(simId, out var opSim))
            {
                operationSim = opSim;
            }

            var profileSim = GetMsisdnAssetPoolListProfile.ResolveLinkedSim(asset);
            var exposePairing = MsisdnAssetKitResolver.ShouldExposePoolPairing(asset.PoolStatus);
            var (iccid, imsi) = MsisdnAssetKitResolver.ResolveForPoolDisplay(
                asset, profileSim, operationSim, simsByIccid);
            var lineLookup = MsisdnAssetLineTypeResolver.ResolveLookup(asset);
            var dto = _mapper.Map<GetMsisdnAssetPoolListDto>(asset);
            return dto with
            {
                Iccid = iccid,
                Imsi = imsi,
                ProductName = exposePairing ? dto.ProductName : null,
                CompatibleSubscriptionTypeId = MsisdnAssetLineTypeResolver.ResolveTypeId(asset),
                CompatibleSubscriptionTypeCode = lineLookup?.Code,
                CompatibleSubscriptionTypeNameAr = lineLookup?.NameAr,
                CompatibleSubscriptionTypeNameEn = lineLookup?.NameEn,
            };
        }).ToList();

        return new GetMsisdnAssetPoolListResult { Data = data };
    }

    /// <summary>Accepts enum name (<c>Available</c>) or numeric value (<c>0</c>) from query strings.</summary>
    private static MsisdnPoolStatus? ResolvePoolStatusFilter(string status)
    {
        if (Enum.TryParse<MsisdnPoolStatus>(status, true, out var byName))
        {
            return byName;
        }

        if (int.TryParse(status, out var numeric) && Enum.IsDefined(typeof(MsisdnPoolStatus), numeric))
        {
            return (MsisdnPoolStatus)numeric;
        }

        return null;
    }
}
