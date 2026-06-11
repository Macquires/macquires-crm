using Application.Common.Exceptions;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class ReleaseMsisdnReservationResult
{
    public string MsisdnAssetId { get; init; } = "";
    public string? Msisdn { get; init; }
    public MsisdnPoolStatus PoolStatus { get; init; }
    public bool Released { get; init; }
}

public class ReleaseMsisdnReservationRequest : IRequest<ReleaseMsisdnReservationResult>
{
    public string MsisdnAssetId { get; init; } = "";
    public string CustomerId { get; init; } = "";
    public string? ReleasedByUserId { get; init; }
}

public class ReleaseMsisdnReservationValidator : AbstractValidator<ReleaseMsisdnReservationRequest>
{
    public ReleaseMsisdnReservationValidator()
    {
        RuleFor(x => x.MsisdnAssetId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}

public class ReleaseMsisdnReservationHandler : IRequestHandler<ReleaseMsisdnReservationRequest, ReleaseMsisdnReservationResult>
{
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReleaseMsisdnReservationHandler(
        ICommandRepository<MsisdnAsset> msisdnRepository,
        IUnitOfWork unitOfWork)
    {
        _msisdnRepository = msisdnRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReleaseMsisdnReservationResult> Handle(
        ReleaseMsisdnReservationRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await _msisdnRepository.GetAsync(request.MsisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم MSISDN غير موجود.");

        if (asset.PoolStatus != MsisdnPoolStatus.Reserved)
        {
            return new ReleaseMsisdnReservationResult
            {
                MsisdnAssetId = asset.Id,
                Msisdn = asset.Msisdn,
                PoolStatus = asset.PoolStatus,
                Released = false
            };
        }

        if (!string.Equals(asset.ReservedForCustomerId, request.CustomerId, StringComparison.Ordinal))
        {
            throw new BusinessRuleViolationException("الرقم محجوز لعميل آخر.");
        }

        asset.ReleaseReservation();
        asset.UpdatedById = request.ReleasedByUserId;
        _msisdnRepository.Update(asset);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new ReleaseMsisdnReservationResult
        {
            MsisdnAssetId = asset.Id,
            Msisdn = asset.Msisdn,
            PoolStatus = asset.PoolStatus,
            Released = true
        };
    }
}
