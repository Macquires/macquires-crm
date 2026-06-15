using Application.Common.CQS.Queries;
using Application.Common.Distributed;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class ReserveMsisdnForCustomerResult
{
    public string MsisdnAssetId { get; init; } = "";
    public string? Msisdn { get; init; }
    public MsisdnPoolStatus PoolStatus { get; init; }
    public DateTime? ReservedUntilUtc { get; init; }
}

public class ReserveMsisdnForCustomerRequest : IRequest<ReserveMsisdnForCustomerResult>, IRequireAnyPermission
{
    public string MsisdnAssetId { get; init; } = "";
    public string CustomerId { get; init; } = "";

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.ReserveMsisdnAny;
}

public class ReserveMsisdnForCustomerValidator : AbstractValidator<ReserveMsisdnForCustomerRequest>
{
    public ReserveMsisdnForCustomerValidator()
    {
        RuleFor(x => x.MsisdnAssetId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}

public class ReserveMsisdnForCustomerHandler : IRequestHandler<ReserveMsisdnForCustomerRequest, ReserveMsisdnForCustomerResult>
{
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITelecomInventoryRulesProvider _inventoryRules;
    private readonly IDistributedLock _distributedLock;
    private readonly IOperatorContext _operator;

    public ReserveMsisdnForCustomerHandler(
        ICommandRepository<MsisdnAsset> msisdnRepository,
        IQueryContext query,
        IUnitOfWork unitOfWork,
        ITelecomInventoryRulesProvider inventoryRules,
        IDistributedLock distributedLock,
        IOperatorContext operatorContext)
    {
        _msisdnRepository = msisdnRepository;
        _query = query;
        _unitOfWork = unitOfWork;
        _inventoryRules = inventoryRules;
        _distributedLock = distributedLock;
        _operator = operatorContext;
    }

    public async Task<ReserveMsisdnForCustomerResult> Handle(
        ReserveMsisdnForCustomerRequest request,
        CancellationToken cancellationToken)
    {
        await using var lockHandle = await _distributedLock.TryAcquireAsync(
            $"msisdn-reserve:{request.MsisdnAssetId}",
            TimeSpan.FromSeconds(30),
            cancellationToken);

        if (lockHandle == null)
        {
            throw new BusinessRuleViolationException("الرقم قيد الحجز من موظف آخر — أعد المحاولة بعد لحظات.");
        }

        var customerExists = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            throw new BusinessRuleViolationException("العميل غير موجود.");
        }

        var asset = await _msisdnRepository.GetAsync(request.MsisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم MSISDN غير موجود.");

        var utcNow = DateTime.UtcNow;
        asset.ReleaseReservationIfExpired(utcNow);

        if (asset.PoolStatus == MsisdnPoolStatus.Reserved
            && asset.ReservedForCustomerId == request.CustomerId
            && asset.ReservedUntilUtc > utcNow)
        {
            ClearDecoupledPairingFields(asset);
            await _unitOfWork.SaveAsync(cancellationToken);
            return new ReserveMsisdnForCustomerResult
            {
                MsisdnAssetId = asset.Id,
                Msisdn = asset.Msisdn,
                PoolStatus = asset.PoolStatus,
                ReservedUntilUtc = asset.ReservedUntilUtc
            };
        }

        var reservationDuration = await _inventoryRules.GetMsisdnReservationDurationAsync(cancellationToken);
        asset.ReserveForCustomer(request.CustomerId, utcNow, reservationDuration);
        ClearDecoupledPairingFields(asset);
        asset.UpdatedById = OperatorActor.RequireUserId(_operator);
        _msisdnRepository.Update(asset);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new ReserveMsisdnForCustomerResult
        {
            MsisdnAssetId = asset.Id,
            Msisdn = asset.Msisdn,
            PoolStatus = asset.PoolStatus,
            ReservedUntilUtc = asset.ReservedUntilUtc
        };
    }

    /// <summary>Reservation locks MSISDN only — SIM pairing happens at confirm/bind.</summary>
    private static void ClearDecoupledPairingFields(MsisdnAsset asset)
    {
        asset.PairedIccid = null;
        asset.PairedImsi = null;
    }
}
