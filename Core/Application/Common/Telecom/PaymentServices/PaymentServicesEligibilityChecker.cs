using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.PaymentServices;

public sealed class PaymentServicesEligibilityChecker : IPaymentServicesEligibilityChecker
{
    private readonly IQueryContext _query;
    private readonly IPaymentServicesFraudTicketService _fraudTickets;

    public PaymentServicesEligibilityChecker(
        IQueryContext query,
        IPaymentServicesFraudTicketService fraudTickets)
    {
        _query = query;
        _fraudTickets = fraudTickets;
    }

    public async Task EnsureCanCreateRechargeAsync(
        string customerId,
        string subscriptionId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new BusinessRuleViolationException("مبلغ الشحن يجب أن يكون أكبر من صفر.");
        }

        var subscription = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Include(s => s.MsisdnAsset)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken)
            ?? throw new BusinessRuleViolationException("الاشتراك غير موجود.");

        var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(p => p.Id == subscription.SubscriberProfileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        if (!string.Equals(profile.CustomerId, customerId, StringComparison.Ordinal))
        {
            throw new BusinessRuleViolationException("الخط لا يتبع هذا المشترك.");
        }

        var msisdn = Customer360WalletBuilder.NormalizeMsisdn(subscription.MsisdnAsset?.Msisdn);
        if (string.IsNullOrEmpty(msisdn))
        {
            throw new BusinessRuleViolationException("لا يوجد رقم خط مرتبط بهذا الاشتراك.");
        }
    }

    public async Task EnsureGatewayReferenceUniqueAsync(
        string gatewayReference,
        string? excludePaymentId,
        CancellationToken cancellationToken = default)
    {
        var reference = gatewayReference.Trim();
        if (string.IsNullOrEmpty(reference))
        {
            throw new BusinessRuleViolationException("رقم مرجع الدفع مطلوب.");
        }

        var duplicate = await _query.TelecomPaymentTransaction.AsNoTracking()
            .IsDeletedEqualTo()
            .AnyAsync(
                p => p.GatewayReference == reference
                    && p.Status == PaymentTransactionStatus.Completed
                    && (excludePaymentId == null || p.Id != excludePaymentId),
                cancellationToken);

        if (duplicate)
        {
            throw new BusinessRuleViolationException(
                "VAL-12-01: مرجع الدفع مستخدم مسبقاً. لا يمكن تكرار الشحن بنفس المرجع.");
        }
    }

    public void EnsureCanConfirm(TelecomPaymentTransaction payment)
    {
        if (payment.Status is PaymentTransactionStatus.Completed or PaymentTransactionStatus.Reversed)
        {
            throw new BusinessRuleViolationException("المعاملة منتهية ولا يمكن تأكيدها.");
        }

        if (payment.Status == PaymentTransactionStatus.Failed)
        {
            throw new BusinessRuleViolationException("المعاملة فاشلة. أنشئ معاملة جديدة.");
        }
    }

    public async Task EnsureCanReverseAsync(TelecomPaymentTransaction payment, CancellationToken cancellationToken = default)
    {
        if (payment.IsReversal)
        {
            throw new BusinessRuleViolationException("لا يمكن عكس حركة تعويضية.");
        }

        if (payment.Status != PaymentTransactionStatus.Completed)
        {
            throw new BusinessRuleViolationException("VAL-12-05: العكس متاح للمعاملات المكتملة فقط.");
        }

        if (payment.ReversedAtUtc.HasValue)
        {
            throw new BusinessRuleViolationException("تم عكس هذه المعاملة مسبقاً.");
        }

        if (!payment.ConfirmedAtUtc.HasValue)
        {
            throw new BusinessRuleViolationException("لا يوجد وقت تأكيد للمعاملة.");
        }

        var elapsed = DateTime.UtcNow - payment.ConfirmedAtUtc.Value;
        if (elapsed > TimeSpan.FromMinutes(PaymentServicesConstants.ReversalWindowMinutes))
        {
            throw new BusinessRuleViolationException(
                $"VAL-12-05: انتهت نافذة العكس ({PaymentServicesConstants.ReversalWindowMinutes} دقيقة).");
        }
    }

    public async Task EnsureRechargeVelocityAsync(
        string msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddMinutes(-PaymentServicesConstants.FraudWindowMinutes);
        var count = await _query.TelecomPaymentTransaction.AsNoTracking()
            .IsDeletedEqualTo()
            .CountAsync(
                p => p.Msisdn == msisdn
                    && p.CreatedAtUtc >= since
                    && (p.Status == PaymentTransactionStatus.Completed
                        || p.Status == PaymentTransactionStatus.PendingGateway),
                cancellationToken);

        if (count < PaymentServicesConstants.MaxRechargesPerFraudWindow)
        {
            return;
        }

        var customerId = await _query.TelecomPaymentTransaction.AsNoTracking()
            .Where(p => p.Msisdn == msisdn && p.CreatedAtUtc >= since)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        var profileId = await _query.TelecomPaymentTransaction.AsNoTracking()
            .Where(p => p.Msisdn == msisdn && p.CreatedAtUtc >= since)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => p.SubscriberProfileId)
            .FirstOrDefaultAsync(cancellationToken);

        await _fraudTickets.EnqueueRechargeVelocityFraudTicketAsync(
            msisdn,
            customerId,
            profileId,
            count,
            actorUserId,
            cancellationToken);

        throw new BusinessRuleViolationException(
            $"VAL-12-04: تجاوز حد الشحن ({PaymentServicesConstants.MaxRechargesPerFraudWindow} عمليات خلال {PaymentServicesConstants.FraudWindowMinutes} دقيقة). تم فتح تذكرة احتيال.");
    }
}
