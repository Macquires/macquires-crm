using Application.Common.Integrations;
using Application.Common.Settings;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.TelecomManager.Events;

/// <summary>SMS notifications for Change GSM Type (CGT) operations.</summary>
public sealed class ChangeGsmNotificationHandler : INotificationHandler<TelecomOperationStatusChangedNotification>
{
    private readonly ISmsGatewayIntegration _sms;
    private readonly IGlobalSettingsProvider _settings;
    private readonly ILogger<ChangeGsmNotificationHandler> _logger;

    public ChangeGsmNotificationHandler(
        ISmsGatewayIntegration sms,
        IGlobalSettingsProvider settings,
        ILogger<ChangeGsmNotificationHandler> logger)
    {
        _sms = sms;
        _settings = settings;
        _logger = logger;
    }

    public async Task Handle(TelecomOperationStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Kind != TelecomOperationKind.ChangeGsmType
            || string.IsNullOrEmpty(notification.Msisdn))
        {
            return;
        }

        if (!await NotificationSmsGate.IsCustomerOpsEnabledAsync(_settings, cancellationToken))
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
            "Change GSM SMS ({From}->{To}) {Msisdn}: {Message}",
            notification.FromStatus,
            notification.ToStatus,
            notification.Msisdn,
            result.Message);
    }

    private static string? ResolveMessage(TelecomOperationStatusChangedNotification n) => n.ToStatus switch
    {
        TelecomOperationStatus.PendingDocuments when n.FromStatus == TelecomOperationStatus.Draft =>
            "تم استلام طلب تحويل نوع الخط (CGT) وهو قيد المراجعة.",
        TelecomOperationStatus.Completed =>
            "تم تحويل نوع خطك بنجاح. شكراً لثقتكم بسيرياتيل.",
        TelecomOperationStatus.Failed =>
            $"تعذّر إكمال تحويل نوع الخط. رقم التتبع: {n.CorrelationId ?? "—"}.",
        TelecomOperationStatus.PendingExternal =>
            "طلب تحويل نوع الخط قيد المزامنة مع أنظمة الشبكة.",
        _ => null
    };
}
