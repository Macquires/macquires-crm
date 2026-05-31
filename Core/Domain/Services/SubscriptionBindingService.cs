using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Services;

/// <summary>ربط ثلاثي: عميل + MSISDN + SIM + عرض — قواعد BSS صارمة.</summary>
public sealed class SubscriptionBindingService : ISubscriptionBindingService
{
    public static readonly TimeSpan DefaultReservationDuration = TimeSpan.FromHours(24);

    public BindSubscriptionResult Bind(BindSubscriptionCommand command, SubscriptionBindingContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (context.Customer.Id != command.CustomerId)
        {
            throw new TelecomBindingRuleException("CustomerMismatch", "العميل لا يطابق سياق الربط.");
        }

        if (context.Customer.Status is CustomerStatus.Closed or CustomerStatus.Blacklisted)
        {
            throw new TelecomBindingRuleException("CustomerBlocked", "لا يمكن تفعيل خط لعميل مغلق أو محظور.");
        }

        ValidateMsisdnAvailable(context.MsisdnAsset, command.CustomerId, context.UtcNow, context.RequireStrictReservation);
        ValidateSimAvailable(context.SimInventory, context.MsisdnAsset.Id);

        if (context.ActiveLineCountForCustomer >= context.MaxLinesAllowed)
        {
            throw new TelecomBindingRuleException("LineLimitExceeded",
                $"تجاوز الحد الأقصى للخطوط المسموحة ({context.MaxLinesAllowed}).");
        }

        var profile = context.SubscriberProfile;
        profile.CustomerId = command.CustomerId;
        profile.Activate();

        var isPrimary = !context.CustomerHasPrimaryLine;
        var subscription = new TelecomSubscription
        {
            SubscriberProfileId = profile.Id,
            MsisdnAssetId = context.MsisdnAsset.Id,
            ProductId = null,
            SubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Prepaid,
            DocumentStatus = TelecomDocumentStatus.Missing,
            IsPrimaryLine = isPrimary
        };

        context.MsisdnAsset.SubscriberProfileId = profile.Id;
        context.MsisdnAsset.TransitionTo(MsisdnPoolStatus.Active);

        context.SimInventory.AssignToProfile(profile.Id);
        context.SimInventory.TransitionTo(SimStatus.Active);

        return new BindSubscriptionResult(
            profile,
            subscription,
            isPrimary,
            context.MsisdnAsset.Msisdn,
            context.SimInventory.Iccid);
    }

    private static void ValidateMsisdnAvailable(MsisdnAsset asset, string customerId, DateTime utcNow, bool requireStrictReservation)
    {
        if (asset.PoolStatus == MsisdnPoolStatus.Quarantined)
        {
            if (asset.QuarantineEndsUtc.HasValue && asset.QuarantineEndsUtc > utcNow)
            {
                throw new TelecomBindingRuleException("MsisdnQuarantined",
                    $"الرقم في فترة حجر حتى {asset.QuarantineEndsUtc:yyyy-MM-dd HH:mm} UTC.");
            }
        }

        if (requireStrictReservation && asset.PoolStatus == MsisdnPoolStatus.Available)
        {
            throw new TelecomBindingRuleException("MsisdnNotReserved",
                "يجب حجز الرقم للعميل قبل التفعيل (Reserved).");
        }

        if (!requireStrictReservation && asset.PoolStatus == MsisdnPoolStatus.Available)
        {
            return;
        }

        if (asset.PoolStatus == MsisdnPoolStatus.Reserved
            && asset.ReservedForCustomerId == customerId
            && asset.ReservedUntilUtc.HasValue
            && asset.ReservedUntilUtc > utcNow)
        {
            return;
        }

        if (asset.PoolStatus == MsisdnPoolStatus.Reserved
            && asset.ReservedUntilUtc.HasValue
            && asset.ReservedUntilUtc <= utcNow)
        {
            throw new TelecomBindingRuleException("ReservationExpired", "انتهت مدة حجز الرقم — أعد الحجز أو اختر رقماً آخر.");
        }

        throw new TelecomBindingRuleException("MsisdnNotAvailable",
            $"الرقم غير متاح للتفعيل (الحالة: {asset.PoolStatus}).");
    }

    private static void ValidateSimAvailable(SimInventory sim, string msisdnAssetId)
    {
        if (sim.Status != SimStatus.Available)
        {
            throw new TelecomBindingRuleException("SimNotAvailable",
                $"الشريحة غير متاحة (الحالة: {sim.Status}).");
        }

        if (!string.IsNullOrEmpty(sim.SubscriberProfileId))
        {
            throw new TelecomBindingRuleException("SimAlreadyPaired", "الشريحة مرتبطة بملف مشترك آخر.");
        }
    }
}
