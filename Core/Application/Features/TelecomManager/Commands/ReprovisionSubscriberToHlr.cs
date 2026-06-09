using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Security;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public sealed class ReprovisionSubscriberToHlrRequest : IRequest<HlrReprovisionResult>, IRequirePermission
{
    public string SubscriberProfileId { get; init; } = "";
    public string? ActorUserId { get; init; }
    public string PermissionKey => PermissionCatalog.TelecomNetworkHlrResync;
}

public sealed class ReprovisionSubscriberToHlrValidator : AbstractValidator<ReprovisionSubscriberToHlrRequest>
{
    public ReprovisionSubscriberToHlrValidator() => RuleFor(x => x.SubscriberProfileId).NotEmpty();
}

public sealed class ReprovisionSubscriberToHlrHandler : IRequestHandler<ReprovisionSubscriberToHlrRequest, HlrReprovisionResult>
{
    private readonly IQueryContext _query;
    private readonly INetworkProvisioningService _network;
    private readonly IHLRLiveStatusService _hlr;
    private readonly IUserAuditService _audit;

    public ReprovisionSubscriberToHlrHandler(
        IQueryContext query,
        INetworkProvisioningService network,
        IHLRLiveStatusService hlr,
        IUserAuditService audit)
    {
        _query = query;
        _network = network;
        _hlr = hlr;
        _audit = audit;
    }

    public async Task<HlrReprovisionResult> Handle(
        ReprovisionSubscriberToHlrRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(p => p.Id == request.SubscriberProfileId, cancellationToken)
            ?? throw new InvalidOperationException("Subscriber profile not found.");

        var line = await (
            from s in _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            join m in _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo() on s.MsisdnAssetId equals m.Id
            where s.SubscriberProfileId == profile.Id
            orderby s.IsPrimaryLine descending
            select new
            {
                s.Id,
                m.Msisdn,
                m.PairedIccid,
                m.PairedImsi,
                s.SubscriptionTypeId,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No active MSISDN for profile.");

        var msisdn = line.Msisdn ?? throw new InvalidOperationException("MSISDN missing.");
        var iccid = (line.PairedIccid ?? "").Trim();
        var imsi = (line.PairedImsi ?? "").Trim();

        if (string.IsNullOrEmpty(iccid) || string.IsNullOrEmpty(imsi))
        {
            throw new InvalidOperationException("ICCID/IMSI مطلوبان لإعادة التزويد على HLR.");
        }

        var subscriptionTypeCode = await _query.TelecomSubscriptionTypeLookup.AsNoTracking()
            .Where(t => t.Id == line.SubscriptionTypeId)
            .Select(t => t.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var correlationId = Guid.NewGuid().ToString("N");
        var provision = await _network.ProvisionAsync(
            new NetworkProvisionRequest(
                OperationId: line.Id,
                OperationNumber: $"HLR-RP-{msisdn[^4..]}",
                Msisdn: msisdn,
                Iccid: iccid,
                CorrelationId: correlationId,
                Kind: TelecomOperationKind.NewActivation,
                Imsi: imsi,
                SubscriptionTypeCode: subscriptionTypeCode),
            cancellationToken);

        if (!provision.Success)
        {
            return new HlrReprovisionResult(false, provision.Message, null, null);
        }

        var result = await _hlr.ReprovisionSubscriberAsync(
            new HlrReprovisionRequest(profile.Id, msisdn, imsi, iccid, request.ActorUserId),
            cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.ActorUserId ?? "system",
                ActionType = UserAuditActionTypes.NetworkCommandExecuted,
                EntityType = "HLR",
                EntityId = profile.Id,
                SummaryAr = $"إعادة تزويد HLR للخط {msisdn}",
                Payload = new
                {
                    msisdn,
                    iccid,
                    imsi,
                    result.LogEntry,
                    result.HlrSubscriberState,
                },
            },
            cancellationToken);

        return result;
    }
}
