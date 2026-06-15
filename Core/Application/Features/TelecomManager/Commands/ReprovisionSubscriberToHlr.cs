using System.Diagnostics;

using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public sealed class ReprovisionSubscriberToHlrRequest : IRequest<HlrReprovisionResult>, IRequirePermission
{
    public string SubscriberProfileId { get; init; } = "";

    /// <summary>When set, reprovisions this MSISDN asset instead of the primary line on the profile.</summary>
    public string? MsisdnAssetId { get; init; }

    public string? Msisdn { get; init; }

    public string PermissionKey => PermissionCatalog.TelecomNetworkHlrResync;
}

public sealed class ReprovisionSubscriberToHlrValidator : AbstractValidator<ReprovisionSubscriberToHlrRequest>
{
    public ReprovisionSubscriberToHlrValidator() => RuleFor(x => x.SubscriberProfileId).NotEmpty();
}

public sealed class ReprovisionSubscriberToHlrHandler : IRequestHandler<ReprovisionSubscriberToHlrRequest, HlrReprovisionResult>
{
    private readonly IQueryContext _query;
    private readonly IHLRLiveStatusService _hlr;
    private readonly ITelecomIntegrationLogWriter _integrationLog;
    private readonly IUserAuditService _audit;
    private readonly IOperatorContext _operator;

    public ReprovisionSubscriberToHlrHandler(
        IQueryContext query,
        IHLRLiveStatusService hlr,
        ITelecomIntegrationLogWriter integrationLog,
        IUserAuditService audit,
        IOperatorContext operatorContext)
    {
        _query = query;
        _hlr = hlr;
        _integrationLog = integrationLog;
        _audit = audit;
        _operator = operatorContext;
    }

    public async Task<HlrReprovisionResult> Handle(
        ReprovisionSubscriberToHlrRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);

        var profileExists = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(p => p.Id == request.SubscriberProfileId, cancellationToken);
        if (!profileExists)
        {
            throw new InvalidOperationException("Subscriber profile not found.");
        }

        var line = await SubscriberLineResolver.ResolveKitAsync(
            _query,
            request.SubscriberProfileId,
            request.MsisdnAssetId,
            request.Msisdn,
            cancellationToken: cancellationToken);

        var requestPayload =
            $"{TelecomBssOperations.HlrReprovisionSubscriber} MSISDN={line.Msisdn} IMSI={line.Imsi} ICCID={line.Iccid}";
        var sw = Stopwatch.StartNew();

        var result = await _hlr.ReprovisionSubscriberAsync(
            new HlrReprovisionRequest(line.SubscriberProfileId, line.Msisdn, line.Imsi, line.Iccid, actorUserId),
            cancellationToken);

        if (!result.Success)
        {
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_HLR,
                TelecomBssOperations.HlrReprovisionSubscriber,
                line.Msisdn,
                requestPayload,
                result.Message,
                false,
                "ERROR",
                sw.ElapsedMilliseconds,
                cancellationToken);
            return result;
        }

        await _integrationLog.WriteAsync(
            TelecomIntegrationSystem.Huawei_HLR,
            TelecomBssOperations.HlrReprovisionSubscriber,
            line.Msisdn,
            requestPayload,
            result.LogEntry ?? result.Message,
            true,
            "200",
            sw.ElapsedMilliseconds,
            cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId,
                ActionType = UserAuditActionTypes.NetworkCommandExecuted,
                EntityType = "HLR",
                EntityId = line.SubscriberProfileId,
                SummaryAr = $"إعادة تزويد HLR للخط {line.Msisdn}",
                Payload = new
                {
                    line.Msisdn,
                    line.Iccid,
                    line.Imsi,
                    result.LogEntry,
                    result.HlrSubscriberState,
                },
            },
            cancellationToken);

        return result;
    }
}
