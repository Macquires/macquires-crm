using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class UpdateCustomerPrimaryTelecomLineResult
{
    public string CustomerId { get; init; } = "";
    public string? NewMsisdn { get; init; }
    /// <summary>Integration code of the resulting type (e.g. PREPAID).</summary>
    public string? NewSubscriptionTypeCode { get; init; }
}

public class UpdateCustomerPrimaryTelecomLineRequest : IRequest<UpdateCustomerPrimaryTelecomLineResult>
{
    public string CustomerId { get; init; } = "";
    /// <summary>When null, MSISDN is not changed.</summary>
    public string? PrimaryMsisdn { get; init; }
    /// <summary>When null, subscription type is not changed.</summary>
    public string? PrimarySubscriptionTypeId { get; init; }
    /// <summary>When provided, updates this specific subscription instead of resolving the default primary one.</summary>
    public string? SubscriptionId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateCustomerPrimaryTelecomLineValidator : AbstractValidator<UpdateCustomerPrimaryTelecomLineRequest>
{
    public UpdateCustomerPrimaryTelecomLineValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.PrimaryMsisdn != null || !string.IsNullOrWhiteSpace(x.PrimarySubscriptionTypeId) || !string.IsNullOrWhiteSpace(x.SubscriptionId))
            .WithMessage("يجب إرسال رقم جديد أو نوع خط جديد أو معرف اشتراك على الأقل.");
        When(x => x.PrimaryMsisdn != null, () =>
        {
            RuleFor(x => x.PrimaryMsisdn!)
                .Must(v => TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(v) != null)
                .WithMessage("صيغة رقم الموبايل غير صالحة. استخدم 09xxxxxxxx أو +9639xxxxxxxx.");
        });
        When(x => !string.IsNullOrWhiteSpace(x.PrimarySubscriptionTypeId), () =>
        {
            RuleFor(x => x.PrimarySubscriptionTypeId!).MaximumLength(50);
        });
    }
}

public class UpdateCustomerPrimaryTelecomLineHandler
    : IRequestHandler<UpdateCustomerPrimaryTelecomLineRequest, UpdateCustomerPrimaryTelecomLineResult>
{
    private const string DuplicateMsisdnAr =
        "عذراً، هذا الرقم (MSISDN) مرتبط حالياً بمشترك أو خط آخر.";

    private const string NoPrimaryLineAr =
        "لا يوجد خط أساسي (اشتراك برقم) مرتبط بهذا المشترك.";

    private const string CustomerNotFoundAr = "المشترك غير موجود.";

    private const string NoEffectiveChangeAr = "لم يتم تغيير أي قيمة.";

    private readonly IQueryContext _query;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<TelecomMsisdnChangeLog> _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITelecomDirectorySync _directorySync;

    public UpdateCustomerPrimaryTelecomLineHandler(
        IQueryContext query,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<TelecomMsisdnChangeLog> auditRepository,
        IUnitOfWork unitOfWork,
        ITelecomDirectorySync directorySync)
    {
        _query = query;
        _msisdnRepository = msisdnRepository;
        _subscriptionRepository = subscriptionRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _directorySync = directorySync;
    }

    public async Task<UpdateCustomerPrimaryTelecomLineResult> Handle(
        UpdateCustomerPrimaryTelecomLineRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = (request.CustomerId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(customerId))
        {
            throw new BusinessRuleViolationException(CustomerNotFoundAr);
        }

        var customer = await _query.Customer
            .AsNoTracking()
            .IsDeletedEqualTo()
            .Include(c => c.SubscriberProfiles)
            .ThenInclude(sp => sp.Subscriptions)
            .ThenInclude(s => s.MsisdnAsset)
            .Include(c => c.SubscriberProfiles)
            .ThenInclude(sp => sp.Subscriptions)
            .ThenInclude(s => s.SubscriptionTypeLookup)
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer == null)
        {
            throw new BusinessRuleViolationException(CustomerNotFoundAr);
        }

        TelecomSubscription? targetSubscription = null;
        MsisdnAsset? targetMsisdnAsset = null;
        string? targetSubscriberProfileId = null;

        var cleanSubscriptionId = (request.SubscriptionId ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(cleanSubscriptionId))
        {
            var match = customer.SubscriberProfiles
                .Where(sp => !sp.IsDeleted)
                .SelectMany(sp => sp.Subscriptions ?? Enumerable.Empty<TelecomSubscription>())
                .FirstOrDefault(s => s.Id == cleanSubscriptionId && !s.IsDeleted);

            if (match == null)
            {
                throw new BusinessRuleViolationException("الاشتراك المحدد غير تابع للمشترك الحالي.");
            }

            targetSubscription = match;
            targetMsisdnAsset = match.MsisdnAsset;
            targetSubscriberProfileId = match.SubscriberProfileId;
        }
        else
        {
            var resolution = CustomerPrimaryTelecomLineResolver.TryResolve(customer);
            if (resolution == null)
            {
                throw new BusinessRuleViolationException(NoPrimaryLineAr);
            }

            targetSubscription = resolution.Subscription;
            targetMsisdnAsset = resolution.MsisdnAsset;
            targetSubscriberProfileId = resolution.SubscriberProfileId;
        }

        if (targetMsisdnAsset == null || targetSubscription == null)
        {
            throw new BusinessRuleViolationException("تعذر تحميل بيانات خط الهاتف أو الاشتراك المحدّد.");
        }

        var oldMsisdn = targetMsisdnAsset.Msisdn;
        var oldTypeCode = targetSubscription.SubscriptionTypeLookup?.Code
                          ?? targetSubscription.SubscriptionTypeId;

        string? newMsisdnCanonical = null;
        if (request.PrimaryMsisdn != null)
        {
            newMsisdnCanonical = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.PrimaryMsisdn);
            if (newMsisdnCanonical == null)
            {
                throw new BusinessRuleViolationException(
                    "صيغة رقم الموبايل غير صالحة. استخدم 09xxxxxxxx أو +9639xxxxxxxx.");
            }
        }

        TelecomSubscriptionTypeLookup? newLookup = null;
        var newTypeId = (request.PrimarySubscriptionTypeId ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(newTypeId))
        {
            newLookup = await _query.TelecomSubscriptionTypeLookup
                .AsNoTracking()
                .IsDeletedEqualTo()
                .FirstOrDefaultAsync(x => x.Id == newTypeId && x.IsActive, cancellationToken);

            if (newLookup == null)
            {
                throw new BusinessRuleViolationException("نوع الخط غير صالح أو غير مفعّل.");
            }
        }

        var msisdnChanging = newMsisdnCanonical != null &&
                             !string.Equals(newMsisdnCanonical, oldMsisdn, StringComparison.Ordinal);
        var typeChanging = newLookup != null &&
                           !string.Equals(newLookup.Id, targetSubscription.SubscriptionTypeId, StringComparison.Ordinal);

        var asset = await _msisdnRepository.GetAsync(targetMsisdnAsset.Id, cancellationToken)
                    ?? throw new BusinessRuleViolationException("أصل الرقم غير موجود.");
        var subscription = await _subscriptionRepository.GetAsync(targetSubscription.Id, cancellationToken)
                           ?? throw new BusinessRuleViolationException("الاشتراك غير موجود.");

        var primaryChanging = !subscription.IsPrimaryLine;

        if (!msisdnChanging && !typeChanging && !primaryChanging)
        {
            var finalCodeCached = await _query.TelecomSubscriptionTypeLookup
                .AsNoTracking()
                .Where(x => x.Id == subscription.SubscriptionTypeId)
                .Select(x => x.Code)
                .FirstOrDefaultAsync(cancellationToken) ?? subscription.SubscriptionTypeId;

            return new UpdateCustomerPrimaryTelecomLineResult
            {
                CustomerId = customerId,
                NewMsisdn = oldMsisdn,
                NewSubscriptionTypeCode = finalCodeCached,
            };
        }

        if (msisdnChanging && newMsisdnCanonical != null)
        {
            var taken = await _query.MsisdnAsset
                .AsNoTracking()
                .IsDeletedEqualTo()
                .AnyAsync(
                    m => m.Msisdn == newMsisdnCanonical && m.Id != targetMsisdnAsset.Id,
                    cancellationToken);

            if (taken)
            {
                throw new BusinessRuleViolationException(DuplicateMsisdnAr);
            }

            asset.Msisdn = newMsisdnCanonical;
            asset.UpdatedById = request.UpdatedById;
            _msisdnRepository.Update(asset);
        }

        if (typeChanging)
        {
            throw new BusinessRuleViolationException(
                "تغيير نوع الخط (مسبق الدفع / فاتورة / هجين) محظور من هنا. استخدم معالج «تحويل نوع الخط» (CGT-) من Customer 360 أو Telecom Hub.");
        }

        if (primaryChanging)
        {
            var otherSubs = await _subscriptionRepository.GetQuery()
                .Where(s => s.SubscriberProfile != null && s.SubscriberProfile.CustomerId == customerId && s.Id != subscription.Id)
                .ToListAsync(cancellationToken);

            foreach (var other in otherSubs)
            {
                if (other.IsPrimaryLine)
                {
                    other.IsPrimaryLine = false;
                    other.UpdatedById = request.UpdatedById;
                    _subscriptionRepository.Update(other);
                }
            }

            subscription.IsPrimaryLine = true;
            subscription.UpdatedById = request.UpdatedById;
            _subscriptionRepository.Update(subscription);
        }

        if (msisdnChanging && newMsisdnCanonical != null)
        {
            var syncResult = await _directorySync.NotifyMsisdnChangedAsync(
                new TelecomDirectoryMsisdnChangeRequest(
                    customerId,
                    asset.Id,
                    oldMsisdn,
                    newMsisdnCanonical,
                    CorrelationId: null),
                cancellationToken);

            var log = new TelecomMsisdnChangeLog
            {
                CustomerId = customerId,
                SubscriberProfileId = targetSubscriberProfileId,
                TelecomSubscriptionId = subscription.Id,
                MsisdnAssetId = asset.Id,
                OldMsisdn = oldMsisdn,
                NewMsisdn = newMsisdnCanonical,
                OldSubscriptionType = typeChanging ? oldTypeCode : null,
                NewSubscriptionType = typeChanging && newLookup != null ? newLookup.Code : null,
                ExternalSyncSuccess = syncResult.Success,
                ExternalSyncMessage = syncResult.Message,
                CreatedById = request.UpdatedById,
            };

            await _auditRepository.CreateAsync(log, cancellationToken);
        }
        else if (typeChanging && newLookup != null)
        {
            var log = new TelecomMsisdnChangeLog
            {
                CustomerId = customerId,
                SubscriberProfileId = targetSubscriberProfileId,
                TelecomSubscriptionId = subscription.Id,
                MsisdnAssetId = asset.Id,
                OldMsisdn = oldMsisdn,
                NewMsisdn = oldMsisdn,
                OldSubscriptionType = oldTypeCode,
                NewSubscriptionType = newLookup.Code,
                ExternalSyncSuccess = true,
                ExternalSyncMessage = "نوع خط فقط (بدون تغيير MSISDN).",
                CreatedById = request.UpdatedById,
            };

            await _auditRepository.CreateAsync(log, cancellationToken);
        }
        else if (primaryChanging)
        {
            var log = new TelecomMsisdnChangeLog
            {
                CustomerId = customerId,
                SubscriberProfileId = targetSubscriberProfileId,
                TelecomSubscriptionId = subscription.Id,
                MsisdnAssetId = asset.Id,
                OldMsisdn = oldMsisdn,
                NewMsisdn = oldMsisdn,
                OldSubscriptionType = oldTypeCode,
                NewSubscriptionType = oldTypeCode,
                ExternalSyncSuccess = true,
                ExternalSyncMessage = "تعديل الخط الأساسي للمشترك.",
                CreatedById = request.UpdatedById,
            };

            await _auditRepository.CreateAsync(log, cancellationToken);
        }

        await _unitOfWork.SaveAsync(cancellationToken);

        var finalCode = await _query.TelecomSubscriptionTypeLookup
            .AsNoTracking()
            .Where(x => x.Id == subscription.SubscriptionTypeId)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken) ?? subscription.SubscriptionTypeId;

        return new UpdateCustomerPrimaryTelecomLineResult
        {
            CustomerId = customerId,
            NewMsisdn = msisdnChanging ? newMsisdnCanonical : oldMsisdn,
            NewSubscriptionTypeCode = finalCode,
        };
    }
}
