using Application.Common.Integrations;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.TelecomManager.Events;

/// <summary>SMS notifications for Selling Line (Blueprint page 22).</summary>
public sealed class SellingLineNotificationHandler : INotificationHandler<TelecomOperationStatusChangedNotification>
{
    private readonly ISmsGatewayIntegration _sms;
    private readonly ILogger<SellingLineNotificationHandler> _logger;

    public SellingLineNotificationHandler(ISmsGatewayIntegration sms, ILogger<SellingLineNotificationHandler> logger)
    {
        _sms = sms;
        _logger = logger;
    }

    public async Task Handle(TelecomOperationStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Kind != TelecomOperationKind.NewActivation
            || string.IsNullOrEmpty(notification.Msisdn))
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
            "Selling line SMS ({From}->{To}) {Msisdn}: {Message}",
            notification.FromStatus,
            notification.ToStatus,
            notification.Msisdn,
            result.Message);
    }

    private static string? ResolveMessage(TelecomOperationStatusChangedNotification n) => n.ToStatus switch
    {
        TelecomOperationStatus.PendingDocuments when n.FromStatus == TelecomOperationStatus.Draft =>
            "تم استلام طلب التفعيل الخاص بكم وهو قيد المعالجة الآن.",
        TelecomOperationStatus.Failed =>
            $"تعذّر إكمال تفعيل خطك. رقم التتبع: {n.CorrelationId ?? "—"}. يرجى مراجعة الفرع.",
        TelecomOperationStatus.PendingExternal =>
            "طلب التفعيل قيد المزامنة مع أنظمة الشبكة. سنبلغكم عند الاكتمال.",
        _ => null
    };
}
