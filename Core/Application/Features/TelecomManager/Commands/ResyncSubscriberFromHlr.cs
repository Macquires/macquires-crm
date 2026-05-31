using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class ResyncSubscriberFromHlrRequest : IRequest<HlrResyncResult>
{
    public string SubscriberProfileId { get; init; } = "";
    public string? ActorUserId { get; init; }
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

    public ResyncSubscriberFromHlrHandler(
        IQueryContext query,
        ICommandRepository<Domain.Entities.SubscriberProfile> profileRepository,
        IHLRLiveStatusService hlr,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _profileRepository = profileRepository;
        _hlr = hlr;
        _unitOfWork = unitOfWork;
    }

    public async Task<HlrResyncResult> Handle(ResyncSubscriberFromHlrRequest request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetAsync(request.SubscriberProfileId, cancellationToken)
            ?? throw new InvalidOperationException("Subscriber profile not found.");

        var msisdn = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfileId == profile.Id)
            .Select(s => s.MsisdnAsset!.Msisdn)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No MSISDN for profile.");

        var live = await _hlr.QueryLiveStatusAsync(msisdn, profile.OperationalStatus.ToString(), cancellationToken);

        if (live.HlrSubscriberState == "ACTIVE" && profile.OperationalStatus != SubscriberOperationalStatus.Active)
        {
            profile.Activate();
        }
        else if (live.HlrSubscriberState == "SUSPENDED")
        {
            profile.Suspend();
        }

        _profileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        return await _hlr.ResyncFromHlrAsync(
            new HlrResyncRequest(profile.Id, msisdn, null, request.ActorUserId),
            cancellationToken);
    }
}
