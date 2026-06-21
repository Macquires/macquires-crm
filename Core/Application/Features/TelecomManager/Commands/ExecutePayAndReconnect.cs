using Application.Common.CQS.Commands;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.BackOffice;
using Application.Common.Telecom.Reconnect;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class ExecutePayAndReconnectResult
{
    public string PaymentReference { get; set; } = string.Empty;
    public string ReconnectReference { get; set; } = string.Empty;
    public string? MessageAr { get; set; }
    public string? MessageEn { get; set; }
}

public class ExecutePayAndReconnectRequest : IRequest<ExecutePayAndReconnectResult>, IRequireAnyPermission
{
    public string CustomerId { get; init; } = null!;
    public string SubscriberProfileId { get; init; } = null!;
    public string Msisdn { get; init; } = null!;

    public IReadOnlyList<string> PermissionKeys => new[] { "telecom.line.reconnect_request", "telecom.line.reconnect", "telecom.hub.frontline" };
}

public class ExecutePayAndReconnectHandler : IRequestHandler<ExecutePayAndReconnectRequest, ExecutePayAndReconnectResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ICommandRepository<TelecomPaymentTransaction> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IQueryContext _queryContext;
    private readonly IOperatorContext _operator;

    public ExecutePayAndReconnectHandler(
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<TelecomPaymentTransaction> paymentRepository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IQueryContext queryContext,
        IOperatorContext operatorContext)
    {
        _operationRepository = operationRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _queryContext = queryContext;
        _operator = operatorContext;
    }

    public async Task<ExecutePayAndReconnectResult> Handle(
        ExecutePayAndReconnectRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _operator.UserId
            ?? throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ العملية.");

        var profile = await _queryContext.SubscriberProfile
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.SubscriberProfileId && !x.IsDeleted, cancellationToken)
            ?? throw new BusinessRuleViolationException("الاشتراك غير موجود.");

        var msisdnAsset = await _queryContext.MsisdnAsset
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SubscriberProfileId == request.SubscriberProfileId && !x.IsDeleted, cancellationToken)
            ?? throw new BusinessRuleViolationException("الرقم غير موجود.");

        var amountToPay = await ResolveOutstandingBalanceAsync(request.Msisdn, cancellationToken);

        string paymentNumber;
        if (amountToPay > 0)
        {
            paymentNumber = await _numberSequenceService.GenerateNumberAsync(
                "TelecomPayment", "PAY-C360-", "", useDate: false, cancellationToken: cancellationToken);

            var payment = new TelecomPaymentTransaction
            {
                CustomerId = request.CustomerId,
                SubscriberProfileId = request.SubscriberProfileId,
                Msisdn = request.Msisdn,
                Amount = amountToPay,
                Currency = "SYP",
                PaymentChannel = PaymentChannel.Cash,
                TransactionType = PaymentTransactionType.BillPay,
                Number = paymentNumber,
                Status = PaymentTransactionStatus.Completed,
                ServiceChannel = PaymentServiceChannel.Showroom,
                ReceiptNumber = paymentNumber,
                BranchId = string.IsNullOrWhiteSpace(_operator.BranchId) ? null : _operator.BranchId.Trim(),
                CreatedById = actorUserId,
                ConfirmedAtUtc = DateTime.UtcNow,
            };

            await _paymentRepository.CreateAsync(payment, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
        }
        else
        {
            paymentNumber = "N/A";
        }

        var reconnectNumber = await _numberSequenceService.GenerateNumberAsync(
            "TelecomOp_Reconnect", "RCN-", "", useDate: false, cancellationToken: cancellationToken);

        var reconnectOp = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.Reconnect,
            Number = reconnectNumber,
            CorrelationId = Guid.CreateVersion7().ToString(),
            Status = TelecomOperationStatus.Draft,
            DocumentStatus = TelecomDocumentStatus.Missing,
            SubscriberProfileId = request.SubscriberProfileId,
            MsisdnAssetId = msisdnAsset.Id,
            Notes = amountToPay > 0
                ? $"Customer360|PayAndReconnect|Auto-generated from payment {paymentNumber}"
                : "Customer360|PayAndReconnect|No payment needed",
            ReconnectReason = amountToPay > 0 ? "LateBillPayment" : "CustomerRequest",
            ClearanceType = amountToPay > 0 ? ReconnectWellKnown.Payment : ReconnectWellKnown.Customer,
            PaymentReference = amountToPay > 0 ? paymentNumber : null,
            BranchId = TelecomDemoMsisdn.IsWellKnown(request.Msisdn.Trim())
                ? null
                : string.IsNullOrWhiteSpace(_operator.BranchId) ? null : _operator.BranchId.Trim(),
            CreatedById = actorUserId,
        };

        if (amountToPay > 0)
        {
            BackOfficeTelecomPipelineState.ApplyBackOfficeRouting(reconnectOp, paidSettlement: true);
        }

        await _operationRepository.CreateAsync(reconnectOp, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new ExecutePayAndReconnectResult
        {
            PaymentReference = paymentNumber,
            ReconnectReference = reconnectNumber,
            MessageAr = amountToPay > 0
                ? $"تم الدفع بقيمة {amountToPay} وإنشاء طلب إعادة التوصيل {reconnectNumber} — بانتظار رفع الهوية واعتماد الباك أوفيس."
                : $"تم إنشاء طلب إعادة التوصيل {reconnectNumber}",
            MessageEn = amountToPay > 0
                ? $"Payment of {amountToPay} completed; reconnect {reconnectNumber} is pending identity upload and back-office approval."
                : $"Reconnect request {reconnectNumber} created",
        };
    }

    private Task<decimal> ResolveOutstandingBalanceAsync(string msisdn, CancellationToken cancellationToken)
    {
        if (msisdn.Trim() == TelecomDemoMsisdn.DebtSubscriber)
        {
            return Task.FromResult(TelecomDemoBaselines.DebtFullPaymentAmountSyp);
        }

        return Task.FromResult(5000m);
    }
}
