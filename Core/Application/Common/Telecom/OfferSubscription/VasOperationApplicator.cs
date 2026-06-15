using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Settings;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OfferSubscription;

public sealed record VasOperationApplyResult(string Msisdn, string ServiceCode, bool Activate, bool Idempotent);

public interface IVasOperationApplicator
{
    Task ValidatePrepaidBalanceForActivateAsync(
        string msisdn,
        string serviceCode,
        CancellationToken cancellationToken = default);

    Task<VasOperationApplyResult> ApplyFromOperationAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class VasOperationApplicator : IVasOperationApplicator
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<SubscriberActiveService> _activeRepository;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IVasProvisioningService _vasProvisioning;
    private readonly IVasBillingIntegration _vasBilling;
    private readonly IGlobalSettingsProvider _globalSettings;
    private readonly IVasCompletionService _vasCompletion;
    private readonly IUnitOfWork _unitOfWork;

    public VasOperationApplicator(
        IQueryContext query,
        ICommandRepository<SubscriberActiveService> activeRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IVasProvisioningService vasProvisioning,
        IVasBillingIntegration vasBilling,
        IGlobalSettingsProvider globalSettings,
        IVasCompletionService vasCompletion,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _activeRepository = activeRepository;
        _operationRepository = operationRepository;
        _vasProvisioning = vasProvisioning;
        _vasBilling = vasBilling;
        _globalSettings = globalSettings;
        _vasCompletion = vasCompletion;
        _unitOfWork = unitOfWork;
    }

    public async Task ValidatePrepaidBalanceForActivateAsync(
        string msisdn,
        string serviceCode,
        CancellationToken cancellationToken = default)
    {
        var subscription = await ResolveSubscriptionByMsisdnAsync(msisdn, cancellationToken);
        var vas = await ResolveVasAsync(serviceCode, cancellationToken);
        await ValidatePrepaidBalanceAsync(subscription, vas, cancellationToken);
    }

    public async Task<VasOperationApplyResult> ApplyFromOperationAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!VasOperationNotes.TryParse(operation.Notes, out var activate, out var serviceCode))
        {
            throw new BusinessRuleViolationException(
                "طلب تعديل الخدمة لا يحدد خدمة VAS (الصيغة: Activate VAS CODE أو Deactivate VAS CODE).");
        }

        var msisdn = await ResolveMsisdnAsync(operation, cancellationToken);
        var subscription = await ResolveSubscriptionByMsisdnAsync(msisdn, cancellationToken);
        var vas = await ResolveVasAsync(serviceCode, cancellationToken);

        if (activate)
        {
            await ValidatePrepaidBalanceAsync(subscription, vas, cancellationToken);

            var alreadyActive = await _query.SubscriberActiveService.AnyAsync(
                x => !x.IsDeleted
                    && x.TelecomSubscriptionId == subscription.Id
                    && x.TelecomValueAddedServiceId == vas.Id
                    && x.Status == SubscriberVasStatus.Active,
                cancellationToken);
            if (alreadyActive)
            {
                return new VasOperationApplyResult(msisdn, serviceCode, true, Idempotent: true);
            }
        }
        else
        {
            var activeRow = await _query.SubscriberActiveService.FirstOrDefaultAsync(
                x => !x.IsDeleted
                    && x.TelecomSubscriptionId == subscription.Id
                    && x.TelecomValueAddedServiceId == vas.Id
                    && x.Status == SubscriberVasStatus.Active,
                cancellationToken);
            if (activeRow == null)
            {
                return new VasOperationApplyResult(msisdn, serviceCode, false, Idempotent: true);
            }
        }

        operation.CorrelationId ??= Guid.CreateVersion7().ToString();
        var provision = await _vasProvisioning.ProvisionVasAsync(
            new VasProvisionRequest(
                msisdn,
                vas.HlrCommandTemplate,
                activate,
                operation.CorrelationId,
                operation.Id),
            cancellationToken);

        if (!provision.Success)
        {
            throw new BusinessRuleViolationException(provision.Message);
        }

        if (activate)
        {
            await UpsertActiveServiceAsync(subscription, vas, msisdn, actorUserId, cancellationToken);
            await DeductPrepaidIfNeededAsync(subscription, vas, cancellationToken);

            if (await _globalSettings.GetBoolAsync(GlobalSettingKeys.VasBillingEnabled, defaultValue: false, cancellationToken))
            {
                var charge = await _vasBilling.ChargeAsync(
                    new VasBillingChargeRequest(
                        operation.Id,
                        msisdn,
                        serviceCode,
                        vas.MonthlyFee,
                        operation.CorrelationId,
                        operation.BranchId),
                    cancellationToken);

                if (!charge.Success)
                {
                    throw new BusinessRuleViolationException(charge.Message ?? "فشل خصم رسوم VAS من CBS.");
                }
            }
        }
        else
        {
            await DeactivateActiveServiceAsync(subscription, vas, actorUserId, cancellationToken);
        }

        operation.ProvisioningResult = "Completed";
        _operationRepository.Update(operation);

        await _vasCompletion.WriteAuditAsync(
            operation.Id,
            serviceCode,
            activate,
            msisdn,
            actorUserId,
            cancellationToken);

        return new VasOperationApplyResult(msisdn, serviceCode, activate, Idempotent: false);
    }

    private async Task<string> ResolveMsisdnAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            throw new BusinessRuleViolationException("رقم الخط غير محدد في طلب تعديل الخدمة.");
        }

        var asset = await _query.MsisdnAsset
            .AsNoTracking()
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(asset.Msisdn ?? string.Empty)
            ?? throw new BusinessRuleViolationException("رقم الخط غير صالح.");

        return msisdn;
    }

    private async Task<TelecomSubscription> ResolveSubscriptionByMsisdnAsync(
        string msisdn,
        CancellationToken cancellationToken)
    {
        var canonical = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(msisdn)
            ?? throw new BusinessRuleViolationException("رقم الخط غير صالح.");

        return await _query.TelecomSubscription
            .Include(s => s.MsisdnAsset)
            .Include(s => s.SubscriberProfile)
            .Include(s => s.SubscriptionTypeLookup)
            .FirstOrDefaultAsync(
                s => !s.IsDeleted && s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == canonical,
                cancellationToken)
            ?? throw new BusinessRuleViolationException("لا يوجد اشتراك لهذا الرقم.");
    }

    private async Task<TelecomValueAddedService> ResolveVasAsync(
        string serviceCode,
        CancellationToken cancellationToken)
    {
        var code = serviceCode.Trim().ToUpperInvariant();
        return await _query.TelecomValueAddedService
            .FirstOrDefaultAsync(
                x => !x.IsDeleted && x.IsActive && x.ServiceCode == code,
                cancellationToken)
            ?? throw new BusinessRuleViolationException($"خدمة VAS '{code}' غير موجودة أو غير فعّالة.");
    }

    private static bool IsPrepaid(TelecomSubscription subscription) =>
        string.Equals(
            subscription.SubscriptionTypeLookup?.Code,
            "PREPAID",
            StringComparison.OrdinalIgnoreCase);

    private static Task ValidatePrepaidBalanceAsync(
        TelecomSubscription subscription,
        TelecomValueAddedService vas,
        CancellationToken cancellationToken)
    {
        if (!IsPrepaid(subscription))
        {
            return Task.CompletedTask;
        }

        var profile = subscription.SubscriberProfile;
        if (profile == null)
        {
            throw new BusinessRuleViolationException("تعذر تفعيل الخدمة: رصيد المشترك غير كافي");
        }

        var balance = profile.PrepaidBalance ?? 0;
        if (balance < vas.MonthlyFee)
        {
            throw new BusinessRuleViolationException("تعذر تفعيل الخدمة: رصيد المشترك غير كافي");
        }

        return Task.CompletedTask;
    }

    private async Task UpsertActiveServiceAsync(
        TelecomSubscription subscription,
        TelecomValueAddedService vas,
        string msisdn,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var existing = await _query.SubscriberActiveService.FirstOrDefaultAsync(
            x => !x.IsDeleted
                && x.TelecomSubscriptionId == subscription.Id
                && x.TelecomValueAddedServiceId == vas.Id,
            cancellationToken);

        if (existing != null)
        {
            existing.Status = SubscriberVasStatus.Active;
            existing.Msisdn = msisdn;
            existing.ActivatedAtUtc = DateTime.UtcNow;
            existing.DeactivatedAtUtc = null;
            existing.UpdatedById = actorUserId;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            _activeRepository.Update(existing);
        }
        else
        {
            await _activeRepository.CreateAsync(
                new SubscriberActiveService
                {
                    TelecomSubscriptionId = subscription.Id,
                    TelecomValueAddedServiceId = vas.Id,
                    Msisdn = msisdn,
                    Status = SubscriberVasStatus.Active,
                    ActivatedAtUtc = DateTime.UtcNow,
                    CreatedById = actorUserId,
                },
                cancellationToken);
        }
    }

    private async Task DeactivateActiveServiceAsync(
        TelecomSubscription subscription,
        TelecomValueAddedService vas,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var row = await _query.SubscriberActiveService.FirstOrDefaultAsync(
            x => !x.IsDeleted
                && x.TelecomSubscriptionId == subscription.Id
                && x.TelecomValueAddedServiceId == vas.Id
                && x.Status == SubscriberVasStatus.Active,
            cancellationToken);
        if (row == null)
        {
            return;
        }

        row.Status = SubscriberVasStatus.Suspended;
        row.DeactivatedAtUtc = DateTime.UtcNow;
        row.UpdatedById = actorUserId;
        row.UpdatedAtUtc = DateTime.UtcNow;
        _activeRepository.Update(row);
    }

    private async Task DeductPrepaidIfNeededAsync(
        TelecomSubscription subscription,
        TelecomValueAddedService vas,
        CancellationToken cancellationToken)
    {
        if (!IsPrepaid(subscription))
        {
            return;
        }

        var profile = subscription.SubscriberProfile
            ?? throw new InvalidOperationException("Subscriber profile not found.");
        profile.PrepaidBalance = (profile.PrepaidBalance ?? 0) - vas.MonthlyFee;
        if (profile.PrepaidBalance < 0)
        {
            profile.PrepaidBalance = 0;
        }

        await Task.CompletedTask;
    }
}
