using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Security;
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

public class ReleaseMsisdnReservationRequest : IRequest<ReleaseMsisdnReservationResult>, IRequireAnyPermission
{
    public string MsisdnAssetId { get; init; } = "";
    public string CustomerId { get; init; } = "";

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.ReserveMsisdnAny;
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
    private readonly IOperatorContext _operator;

    public ReleaseMsisdnReservationHandler(
        ICommandRepository<MsisdnAsset> msisdnRepository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _msisdnRepository = msisdnRepository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
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
        asset.UpdatedById = OperatorActor.RequireUserId(_operator);
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
