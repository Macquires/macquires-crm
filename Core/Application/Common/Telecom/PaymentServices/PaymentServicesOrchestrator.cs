using Application.Common.Audit;

using Application.Common.CQS.Queries;

using Application.Common.Exceptions;

using Application.Common.Extensions;

using Application.Common.Integrations;

using Application.Common.Repositories;

using Application.Common.Security;

using Application.Common.Telecom.Billing;

using Application.Features.NumberSequenceManager;

using Application.Features.TelecomManager.Events;

using Domain.Entities;

using Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;



namespace Application.Common.Telecom.PaymentServices;



public sealed partial class PaymentServicesOrchestrator : IPaymentServicesOrchestrator

{

    private readonly IQueryContext _query;

    private readonly ICommandRepository<TelecomPaymentTransaction> _paymentRepository;

    private readonly ICommandRepository<SubscriberProfile> _profileRepository;

    private readonly IUnitOfWork _unitOfWork;

    private readonly NumberSequenceService _numberSequence;

    private readonly IPaymentServicesEligibilityChecker _eligibility;

    private readonly IPaymentGatewayIntegration _paymentGateway;

    private readonly IBillingRoutingOrchestrator _billingRouting;

    private readonly IUserAuditService _audit;

    private readonly IOperatorContext _operatorContext;

    private readonly IMediator _mediator;



    public PaymentServicesOrchestrator(

        IQueryContext query,

        ICommandRepository<TelecomPaymentTransaction> paymentRepository,

        ICommandRepository<SubscriberProfile> profileRepository,

        IUnitOfWork unitOfWork,

        NumberSequenceService numberSequence,

        IPaymentServicesEligibilityChecker eligibility,

        IPaymentGatewayIntegration paymentGateway,

        IBillingRoutingOrchestrator billingRouting,

        IUserAuditService audit,

        IOperatorContext operatorContext,

        IMediator mediator)

    {

        _query = query;

        _paymentRepository = paymentRepository;

        _profileRepository = profileRepository;

        _unitOfWork = unitOfWork;

        _numberSequence = numberSequence;

        _eligibility = eligibility;

        _paymentGateway = paymentGateway;

        _billingRouting = billingRouting;

        _audit = audit;

        _operatorContext = operatorContext;

        _mediator = mediator;

    }



    public Task<PaymentDraftResult> CreateRechargeDraftAsync(

        string customerId,

        string subscriptionId,

        decimal amount,

        PaymentChannel paymentChannel,

        PaymentServiceChannel serviceChannel,

        string? createdById,

        CancellationToken cancellationToken = default) =>

        CreateDraftCoreAsync(

            customerId,

            subscriptionId,

            PaymentTransactionType.Recharge,

            amount,

            paymentChannel,

            serviceChannel,

            null,

            createdById,

            cancellationToken);



    public async Task<PaymentDraftResult> CreateVoucherRedeemDraftAsync(

        string customerId,

        string subscriptionId,

        string voucherCode,

        PaymentServiceChannel serviceChannel,

        string? createdById,

        CancellationToken cancellationToken = default)

    {

        var validation = await _paymentGateway.ValidateVoucherAsync(

            new VoucherValidationRequest(voucherCode.Trim()),

            cancellationToken);



        if (!validation.Valid || !validation.FaceValue.HasValue)

        {

            throw new BusinessRuleViolationException(validation.MessageAr);

        }



        return await CreateDraftCoreAsync(

            customerId,

            subscriptionId,

            PaymentTransactionType.VoucherRedeem,

            validation.FaceValue.Value,

            PaymentChannel.Voucher,

            serviceChannel,

            voucherCode.Trim(),

            createdById,

            cancellationToken);

    }



    public async Task<PaymentConfirmOrchestrationResult> ConfirmAsync(

        string paymentId,

        string gatewayReference,

        string? confirmedById,

        CancellationToken cancellationToken = default)

    {

        var payment = await _paymentRepository.GetAsync(paymentId, cancellationToken)

            ?? throw new BusinessRuleViolationException("معاملة الدفع غير موجودة.");



        var fromStatus = payment.Status;

        _eligibility.EnsureCanConfirm(payment);

        await _eligibility.EnsureGatewayReferenceUniqueAsync(gatewayReference, payment.Id, cancellationToken);



        if (payment.TransactionType == PaymentTransactionType.VoucherRedeem)

        {

            var code = payment.VoucherCode ?? gatewayReference.Trim();

            var validation = await _paymentGateway.ValidateVoucherAsync(

                new VoucherValidationRequest(code, payment.Amount),

                cancellationToken);

            if (!validation.Valid)

            {

                throw new BusinessRuleViolationException(validation.MessageAr);

            }

        }



        payment.Status = PaymentTransactionStatus.PendingGateway;

        payment.GatewayReference = gatewayReference.Trim();

        payment.UpdatedById = confirmedById ?? _operatorContext.UserId;

        _paymentRepository.Update(payment);

        await _unitOfWork.SaveAsync(cancellationToken);



        if (payment.TransactionType == PaymentTransactionType.VoucherRedeem && !string.IsNullOrEmpty(payment.VoucherCode))

        {

            var redeem = await _paymentGateway.RedeemVoucherAsync(payment.VoucherCode, cancellationToken);

            if (!redeem.Valid)

            {

                await FailPaymentAsync(payment, redeem.MessageAr, fromStatus, confirmedById, cancellationToken);

                return FailedResult(payment, redeem.MessageAr);

            }

        }



        var gatewayResult = await _paymentGateway.ConfirmAsync(

            new PaymentConfirmRequest(

                payment.Id,

                payment.Number,

                payment.Amount,

                payment.PaymentChannel,

                payment.GatewayReference),

            cancellationToken);



        if (!gatewayResult.Success)

        {

            await FailPaymentAsync(payment, gatewayResult.Message, fromStatus, confirmedById, cancellationToken);

            return FailedResult(payment, gatewayResult.Message);

        }



        payment.GatewayTransactionId = gatewayResult.GatewayTransactionId;



        var profile = await _profileRepository.GetAsync(payment.SubscriberProfileId, cancellationToken)

            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");



        var balanceBefore = profile.PrepaidBalance ?? Customer360WalletBuilder.SimulateBalance(payment.Msisdn!);

        payment.BalanceBefore = balanceBefore;



        var subscriptionTypeCode = await _query.TelecomSubscription.AsNoTracking()
            .IsDeletedEqualTo()
            .Where(s => s.Id == payment.TelecomSubscriptionId)
            .Join(
                _query.TelecomSubscriptionTypeLookup.AsNoTracking().IsDeletedEqualTo(),
                s => s.SubscriptionTypeId,
                t => t.Id,
                (_, t) => t.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var rechargeResult = await _billingRouting.RechargeAsync(
            subscriptionTypeCode,
            new BillingRechargeRequest(
                payment.Id,
                payment.Number,
                payment.Msisdn!,
                payment.Amount,
                payment.CorrelationId),
            cancellationToken);

        if (!rechargeResult.Success)
        {
            await FailPaymentAsync(payment, rechargeResult.Message, fromStatus, confirmedById, cancellationToken);
            return FailedResult(payment, rechargeResult.Message);
        }

        var newBalance = rechargeResult.NewBalance ?? (balanceBefore + payment.Amount);

        profile.PrepaidBalance = newBalance;

        _profileRepository.Update(profile);



        payment.Status = PaymentTransactionStatus.Completed;

        payment.BalanceAfter = newBalance;

        payment.ConfirmedAtUtc = DateTime.UtcNow;

        payment.ReceiptNumber = $"RCP-{payment.Number}";

        payment.FailureReason = null;

        payment.UpdatedById = confirmedById ?? _operatorContext.UserId;

        _paymentRepository.Update(payment);

        await _unitOfWork.SaveAsync(cancellationToken);



        await PublishStatusAsync(payment, fromStatus, confirmedById, cancellationToken);



        var actorId = confirmedById ?? _operatorContext.UserId ?? "";

        if (!string.IsNullOrEmpty(actorId))

        {

            var action = payment.TransactionType == PaymentTransactionType.VoucherRedeem

                ? "PaymentServicesVoucherRedeem"

                : "PaymentServicesRecharge";



            await _audit.LogAsync(

                new UserAuditLogRequest

                {

                    ActorUserId = actorId,

                    ActionType = action,

                    EntityType = "TelecomPaymentTransaction",

                    EntityId = payment.Id,

                    SummaryAr = $"شحن {payment.Amount:N0} ل.س للخط {payment.Msisdn} — {payment.Number}",

                    Payload = new

                    {

                        payment.Number,

                        payment.CorrelationId,

                        payment.GatewayReference,

                        payment.VoucherCode,

                        payment.Amount,

                        balanceBefore,

                        newBalance,

                        payment.ReceiptNumber,

                    },

                },

                cancellationToken);

        }



        var messageAr =

            $"تم شحن {payment.Amount:N0} ل.س بنجاح. الرصيد الجديد: {newBalance:N0} ل.س. إيصال: {payment.ReceiptNumber}";



        return new PaymentConfirmOrchestrationResult(

            true,

            payment.Status,

            payment.ReceiptNumber,

            newBalance,

            payment.CorrelationId,

            messageAr,

            payment.Number,

            payment.Msisdn);

    }



    private async Task<PaymentDraftResult> CreateDraftCoreAsync(

        string customerId,

        string subscriptionId,

        PaymentTransactionType type,

        decimal amount,

        PaymentChannel paymentChannel,

        PaymentServiceChannel serviceChannel,

        string? voucherCode,

        string? createdById,

        CancellationToken cancellationToken)

    {

        await _eligibility.EnsureCanCreateRechargeAsync(customerId, subscriptionId, amount, cancellationToken);



        var subscription = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()

            .Include(s => s.MsisdnAsset)

            .FirstAsync(s => s.Id == subscriptionId, cancellationToken);



        var msisdn = Customer360WalletBuilder.NormalizeMsisdn(subscription.MsisdnAsset?.Msisdn)!;

        await _eligibility.EnsureRechargeVelocityAsync(msisdn, createdById, cancellationToken);

        var payment = new TelecomPaymentTransaction

        {

            Number = _numberSequence.GenerateNumber(nameof(TelecomPaymentTransaction), "", "PAY"),

            CorrelationId = Guid.CreateVersion7().ToString(),

            TransactionType = type,

            Status = PaymentTransactionStatus.Draft,

            PaymentChannel = paymentChannel,

            ServiceChannel = serviceChannel,

            Amount = amount,

            CustomerId = customerId,

            SubscriberProfileId = subscription.SubscriberProfileId,

            TelecomSubscriptionId = subscriptionId,

            Msisdn = msisdn,

            VoucherCode = voucherCode,

            CreatedById = createdById,

        };



        await _paymentRepository.CreateAsync(payment, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);



        return new PaymentDraftResult(

            payment.Id,

            payment.Number,

            payment.CorrelationId,

            payment.Status);

    }



    private async Task FailPaymentAsync(

        TelecomPaymentTransaction payment,

        string reason,

        PaymentTransactionStatus fromStatus,

        string? actorId,

        CancellationToken cancellationToken)

    {

        payment.Status = PaymentTransactionStatus.Failed;

        payment.FailureReason = reason;

        payment.UpdatedById = actorId ?? _operatorContext.UserId;

        _paymentRepository.Update(payment);

        await _unitOfWork.SaveAsync(cancellationToken);

        await PublishStatusAsync(payment, fromStatus, actorId, cancellationToken);

    }



    private async Task PublishStatusAsync(

        TelecomPaymentTransaction payment,

        PaymentTransactionStatus fromStatus,

        string? actorId,

        CancellationToken cancellationToken)

    {

        await _mediator.Publish(

            new PaymentTransactionStatusChangedNotification(

                payment.Id,

                payment.Number,

                payment.TransactionType,

                fromStatus,

                payment.Status,

                payment.CorrelationId,

                payment.Msisdn,

                payment.Amount,

                actorId ?? _operatorContext.UserId),

            cancellationToken);

    }



    private static PaymentConfirmOrchestrationResult FailedResult(TelecomPaymentTransaction payment, string message) =>

        new(

            false,

            payment.Status,

            null,

            null,

            payment.CorrelationId,

            message,

            payment.Number,

            payment.Msisdn);

}

