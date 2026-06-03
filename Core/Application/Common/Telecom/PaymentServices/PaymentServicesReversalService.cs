using Application.Common.Audit;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Features.TelecomManager.Events;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Common.Telecom.PaymentServices;

public sealed record PaymentReversalResult(
    bool Success,
    string MessageAr,
    decimal? NewBalance,
    string? PaymentNumber);

public interface IPaymentServicesReversalService
{
    Task<PaymentReversalResult> ReverseAsync(
        string paymentId,
        string reasonCode,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentServicesReversalService : IPaymentServicesReversalService
{
    private readonly ICommandRepository<TelecomPaymentTransaction> _paymentRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentServicesEligibilityChecker _eligibility;
    private readonly IPaymentGatewayIntegration _paymentGateway;
    private readonly IBillingSystemIntegration _billing;
    private readonly IPaymentServicesAuditWriter _auditWriter;
    private readonly IUserAuditService _userAudit;
    private readonly IOperatorContext _operatorContext;
    private readonly IMediator _mediator;

    public PaymentServicesReversalService(
        ICommandRepository<TelecomPaymentTransaction> paymentRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        IUnitOfWork unitOfWork,
        IPaymentServicesEligibilityChecker eligibility,
        IPaymentGatewayIntegration paymentGateway,
        IBillingSystemIntegration billing,
        IPaymentServicesAuditWriter auditWriter,
        IUserAuditService userAudit,
        IOperatorContext operatorContext,
        IMediator mediator)
    {
        _paymentRepository = paymentRepository;
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _eligibility = eligibility;
        _paymentGateway = paymentGateway;
        _billing = billing;
        _auditWriter = auditWriter;
        _userAudit = userAudit;
        _operatorContext = operatorContext;
        _mediator = mediator;
    }

    public async Task<PaymentReversalResult> ReverseAsync(
        string paymentId,
        string reasonCode,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetAsync(paymentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("معاملة الدفع غير موجودة.");

        var fromStatus = payment.Status;
        await _eligibility.EnsureCanReverseAsync(payment, cancellationToken);

        var actor = actorUserId ?? _operatorContext.UserId ?? "";
        var gatewayRef = payment.GatewayReference ?? payment.Number;

        var gwReverse = await _paymentGateway.ReverseAsync(
            new PaymentReverseRequest(payment.Id, payment.Number, gatewayRef, payment.Amount),
            cancellationToken);
        if (!gwReverse.Success)
        {
            throw new BusinessRuleViolationException(gwReverse.Message);
        }

        var profile = await _profileRepository.GetAsync(payment.SubscriberProfileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        var balanceBefore = profile.PrepaidBalance ?? 0m;
        var cbsReverse = await _billing.ReverseRechargeAsync(
            new BillingReverseRechargeRequest(
                payment.Id,
                payment.Number,
                payment.Msisdn!,
                payment.Amount,
                payment.CorrelationId),
            cancellationToken);

        if (!cbsReverse.Success)
        {
            throw new BusinessRuleViolationException(cbsReverse.Message);
        }

        var newBalance = Math.Max(0m, balanceBefore - payment.Amount);
        if (cbsReverse.NewBalance.HasValue)
        {
            newBalance = cbsReverse.NewBalance.Value;
        }

        profile.PrepaidBalance = newBalance;
        _profileRepository.Update(profile);

        payment.Status = PaymentTransactionStatus.Reversed;
        payment.ReversedAtUtc = DateTime.UtcNow;
        payment.ReversalReasonCode = reasonCode.Trim();
        payment.UpdatedById = actor;
        _paymentRepository.Update(payment);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _auditWriter.WriteAsync(
            payment,
            "Reverse",
            fromStatus,
            PaymentTransactionStatus.Reversed,
            balanceBefore,
            newBalance,
            actor,
            reasonCode,
            gwReverse.Message,
            cancellationToken);

        await _mediator.Publish(
            new PaymentTransactionStatusChangedNotification(
                payment.Id,
                payment.Number,
                payment.TransactionType,
                fromStatus,
                PaymentTransactionStatus.Reversed,
                payment.CorrelationId,
                payment.Msisdn,
                payment.Amount,
                actor),
            cancellationToken);

        if (!string.IsNullOrEmpty(actor))
        {
            await _userAudit.LogAsync(
                new UserAuditLogRequest
                {
                    ActorUserId = actor,
                    ActionType = "PaymentServicesReverse",
                    EntityType = "TelecomPaymentTransaction",
                    EntityId = payment.Id,
                    SummaryAr = $"عكس معاملة {payment.Number} — {reasonCode}",
                    Payload = new { payment.Number, reasonCode, balanceBefore, newBalance },
                },
                cancellationToken);
        }

        return new PaymentReversalResult(
            true,
            $"تم عكس المعاملة {payment.Number}. الرصيد بعد العكس: {newBalance:N0} ل.س.",
            newBalance,
            payment.Number);
    }
}
