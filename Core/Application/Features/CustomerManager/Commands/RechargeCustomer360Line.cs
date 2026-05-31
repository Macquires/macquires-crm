using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Commands;

public class RechargeCustomer360LineResult
{
    public bool Success { get; init; }
    public string? Msisdn { get; init; }
    public decimal RechargedAmount { get; init; }
    public decimal NewBalance { get; init; }
    public string Currency { get; init; } = "SYP";
    public string? Message { get; init; }
}

public class RechargeCustomer360LineRequest : IRequest<RechargeCustomer360LineResult>
{
    public string CustomerId { get; init; } = "";
    public string SubscriptionId { get; init; } = "";
    public decimal Amount { get; init; }
}

public class RechargeCustomer360LineValidator : AbstractValidator<RechargeCustomer360LineRequest>
{
    public RechargeCustomer360LineValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("مبلغ الشحن يجب أن يكون أكبر من صفر.");
    }
}

public class RechargeCustomer360LineHandler : IRequestHandler<RechargeCustomer360LineRequest, RechargeCustomer360LineResult>
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAuditService _audit;
    private readonly IOperatorContext _operatorContext;

    public RechargeCustomer360LineHandler(
        IQueryContext query,
        ICommandRepository<SubscriberProfile> profileRepository,
        IUnitOfWork unitOfWork,
        IUserAuditService audit,
        IOperatorContext operatorContext)
    {
        _query = query;
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _operatorContext = operatorContext;
    }

    public async Task<RechargeCustomer360LineResult> Handle(
        RechargeCustomer360LineRequest request,
        CancellationToken cancellationToken)
    {
        var subscription = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Include(s => s.MsisdnAsset)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken)
            ?? throw new InvalidOperationException("الاشتراك غير موجود.");

        var profile = await _profileRepository.GetAsync(subscription.SubscriberProfileId, cancellationToken)
            ?? throw new InvalidOperationException("ملف المشترك غير موجود.");

        if (!string.Equals(profile.CustomerId, request.CustomerId, StringComparison.Ordinal)
            || profile.IsDeleted)
        {
            throw new InvalidOperationException("الخط لا يتبع هذا المشترك.");
        }

        var msisdn = Customer360WalletBuilder.NormalizeMsisdn(subscription.MsisdnAsset?.Msisdn);
        if (string.IsNullOrEmpty(msisdn))
            throw new InvalidOperationException("لا يوجد رقم خط مرتبط بهذا الاشتراك.");

        var current = profile.PrepaidBalance ?? Customer360WalletBuilder.SimulateBalance(msisdn);
        var newBalance = current + request.Amount;
        profile.PrepaidBalance = newBalance;

        _profileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        var actorId = _operatorContext.UserId ?? "";
        if (!string.IsNullOrEmpty(actorId))
        {
            await _audit.LogAsync(
                new UserAuditLogRequest
                {
                    ActorUserId = actorId,
                    ActionType = "Customer360Recharge",
                    EntityType = "Customer360",
                    EntityId = request.CustomerId,
                    SummaryAr = $"شحن رصيد {request.Amount:N0} ل.س للخط {msisdn}",
                    Payload = new
                    {
                        request.SubscriptionId,
                        msisdn,
                        request.Amount,
                        newBalance,
                    },
                },
                cancellationToken);
        }

        return new RechargeCustomer360LineResult
        {
            Success = true,
            Msisdn = msisdn,
            RechargedAmount = request.Amount,
            NewBalance = newBalance,
            Message = $"تم شحن {request.Amount:N0} ل.س بنجاح. الرصيد الجديد: {newBalance:N0} ل.س.",
        };
    }
}
