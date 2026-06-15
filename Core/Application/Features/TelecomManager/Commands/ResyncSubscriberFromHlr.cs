using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class ResyncSubscriberFromHlrRequest : IRequest<HlrResyncResult>, IRequireAnyPermission
{
    public string SubscriberProfileId { get; init; } = "";
    public string? MsisdnAssetId { get; init; }
    public string? Msisdn { get; init; }

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.NetworkHlrAny;
}

public class ResyncSubscriberFromHlrValidator : AbstractValidator<ResyncSubscriberFromHlrRequest>
{
    public ResyncSubscriberFromHlrValidator() => RuleFor(x => x.SubscriberProfileId).NotEmpty();
}

public class ResyncSubscriberFromHlrHandler : IRequestHandler<ResyncSubscriberFromHlrRequest, HlrResyncResult>
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<Domain.Entities.SubscriberProfile> _profileRepository;
    private readonly IHLRLiveStatusService _hlr;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public ResyncSubscriberFromHlrHandler(
        IQueryContext query,
        ICommandRepository<Domain.Entities.SubscriberProfile> profileRepository,
        IHLRLiveStatusService hlr,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _query = query;
        _profileRepository = profileRepository;
        _hlr = hlr;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<HlrResyncResult> Handle(ResyncSubscriberFromHlrRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);

        var profile = await _profileRepository.GetAsync(request.SubscriberProfileId, cancellationToken)
            ?? throw new InvalidOperationException("Subscriber profile not found.");

        var asset = await SubscriberLineResolver.ResolveAssetAsync(
            _query,
            profile.Id,
            request.MsisdnAssetId,
            request.Msisdn,
            cancellationToken);
        var msisdn = asset.Msisdn;

        var live = await _hlr.QueryLiveStatusAsync(msisdn, profile.OperationalStatus.ToString(), cancellationToken);

        if (live.HlrSubscriberState == "ACTIVE" && profile.OperationalStatus != SubscriberOperationalStatus.Active)
        {
            profile.Activate();
        }
        else if (live.HlrSubscriberState == "SUSPENDED")
        {
            profile.Suspend(msisdn);
        }

        _profileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        return await _hlr.ResyncFromHlrAsync(
            new HlrResyncRequest(profile.Id, msisdn, null, actorUserId),
            cancellationToken);
    }
}
