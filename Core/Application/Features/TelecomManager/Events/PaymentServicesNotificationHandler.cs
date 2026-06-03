using Application.Common.Integrations;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.TelecomManager.Events;

/// <summary>SMS for payment / recharge (Blueprint §12 + §70).</summary>
public sealed class PaymentServicesNotificationHandler : INotificationHandler<PaymentTransactionStatusChangedNotification>
{
    private readonly ISmsGatewayIntegration _sms;
    private readonly ILogger<PaymentServicesNotificationHandler> _logger;

    public PaymentServicesNotificationHandler(ISmsGatewayIntegration sms, ILogger<PaymentServicesNotificationHandler> logger)
    {
        _sms = sms;
        _logger = logger;
    }

    public async Task Handle(PaymentTransactionStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.Msisdn))
        {
            return;
        }

        var body = ResolveMessage(notification);
        if (string.IsNullOrEmpty(body))
        {
            return;
        }

        var result = await _sms.SendAsync(notification.Msisdn, body, cancellationToken);
        _logger.LogInformation(
            "Payment SMS PAY {Number} ({From}->{To}) {Msisdn}: {Message}",
            notification.PaymentNumber,
            notification.FromStatus,
            notification.ToStatus,
            notification.Msisdn,
            result.Message);
    }

    private static string? ResolveMessage(PaymentTransactionStatusChangedNotification n)
    {
        if (n.ToStatus == PaymentTransactionStatus.Completed)
        {
            var kind = n.TransactionType == PaymentTransactionType.VoucherRedeem ? "قسيمة" : "شحن";
            return $"تم {kind} رصيدكم بمبلغ {n.Amount:N0} ل.س. رقم العملية {n.PaymentNumber}. التتبع: {n.CorrelationId ?? "—"}.";
        }

        if (n.ToStatus == PaymentTransactionStatus.Failed)
        {
            return $"تعذّر إتمام عملية الدفع {n.PaymentNumber}. رقم التتبع: {n.CorrelationId ?? "—"}. يرجى مراجعة الفرع.";
        }

        if (n.ToStatus == PaymentTransactionStatus.Reversed)
        {
            return $"تم عكس عملية الدفع {n.PaymentNumber} بمبلغ {n.Amount:N0} ل.س. للاستفسار راجع الفرع.";
        }

        return null;
    }
}
