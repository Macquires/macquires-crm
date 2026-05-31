using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom;

public interface ISubscriptionBindingExecutor
{
    Task<BindSubscriptionResult> ExecuteAsync(
        BindSubscriptionCommand command,
        string subscriptionTypeId,
        string? createdById,
        bool requireStrictReservation,
        CancellationToken cancellationToken);
}

public sealed class SubscriptionBindingExecutor : ISubscriptionBindingExecutor
{
    private readonly IQueryContext _query;
    private readonly ISubscriptionBindingService _bindingService;
    private readonly ICustomerLineLimitPolicy _lineLimitPolicy;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<TelecomMsisdnChangeLog> _changeLogRepository;

    public SubscriptionBindingExecutor(
        IQueryContext query,
        ISubscriptionBindingService bindingService,
        ICustomerLineLimitPolicy lineLimitPolicy,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<TelecomMsisdnChangeLog> changeLogRepository)
    {
        _query = query;
        _bindingService = bindingService;
        _lineLimitPolicy = lineLimitPolicy;
        _profileRepository = profileRepository;
        _subscriptionRepository = subscriptionRepository;
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _changeLogRepository = changeLogRepository;
    }

    public async Task<BindSubscriptionResult> ExecuteAsync(
        BindSubscriptionCommand command,
        string subscriptionTypeId,
        string? createdById,
        bool requireStrictReservation,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var customer = await _query.Customer.IsDeletedEqualTo()
            .FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken)
            ?? throw new BusinessRuleViolationException("العميل غير موجود.");

        var msisdn = await _msisdnRepository.GetAsync(command.MsisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم MSISDN غير موجود.");

        var sim = await _simRepository.GetAsync(command.SimInventoryId, cancellationToken)
            ?? throw new BusinessRuleViolationException("الشريحة غير موجودة.");

        msisdn.ReleaseReservationIfExpired(utcNow);

        var profileIds = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == command.CustomerId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var activeLineCount = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .CountAsync(s => profileIds.Contains(s.SubscriberProfileId), cancellationToken);

        var hasPrimary = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(s => profileIds.Contains(s.SubscriberProfileId) && s.IsPrimaryLine, cancellationToken);

        var maxLines = _lineLimitPolicy.GetMaxLinesAllowed(customer, activeLineCount);

        var profile = await _profileRepository.GetAsync(command.SubscriberProfileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        if (profile.CustomerId != command.CustomerId)
        {
            throw new BusinessRuleViolationException("ملف المشترك لا يتبع هذا العميل.");
        }

        var context = new SubscriptionBindingContext
        {
            Customer = customer,
            MsisdnAsset = msisdn,
            SimInventory = sim,
            SubscriberProfile = profile,
            ActiveLineCountForCustomer = activeLineCount,
            MaxLinesAllowed = maxLines,
            CustomerHasPrimaryLine = hasPrimary,
            UtcNow = utcNow,
            RequireStrictReservation = requireStrictReservation
        };

        try
        {
            var result = _bindingService.Bind(command, context);
            result.TelecomSubscription.SubscriptionTypeId = subscriptionTypeId;
            result.TelecomSubscription.CreatedById = createdById;

            _msisdnRepository.Update(msisdn);
            _simRepository.Update(sim);
            _profileRepository.Update(profile);
            await _subscriptionRepository.CreateAsync(result.TelecomSubscription, cancellationToken);

            await _changeLogRepository.CreateAsync(new TelecomMsisdnChangeLog
            {
                CustomerId = command.CustomerId,
                SubscriberProfileId = command.SubscriberProfileId,
                TelecomSubscriptionId = result.TelecomSubscription.Id,
                MsisdnAssetId = command.MsisdnAssetId,
                NewMsisdn = result.Msisdn,
                NewSubscriptionType = subscriptionTypeId,
                ExternalSyncSuccess = true,
                ExternalSyncMessage = "ربط ثلاثي (تفعيل جديد).",
                CreatedById = createdById,
            }, cancellationToken);

            return result;
        }
        catch (TelecomBindingRuleException ex)
        {
            throw new BusinessRuleViolationException(ex.Message);
        }
    }
}
