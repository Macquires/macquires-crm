using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Features.NumberSequenceManager;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.VasManager.Commands;

public class ToggleSubscriberVasServiceResult
{
    public bool IsActive { get; init; }
    public string Msisdn { get; init; } = "";
    public string ServiceCode { get; init; } = "";
    public string? OperationNumber { get; init; }
}

public class ToggleSubscriberVasServiceRequest : IRequest<ToggleSubscriberVasServiceResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.TelecomVasToggle;
    public string Msisdn { get; init; } = "";
    public string ServiceCode { get; init; } = "";
    public VasToggleAction Action { get; init; }
    public string? ActorUserId { get; init; }
    public string? IpAddress { get; init; }
}

public class ToggleSubscriberVasServiceValidator : AbstractValidator<ToggleSubscriberVasServiceRequest>
{
    public ToggleSubscriberVasServiceValidator()
    {
        RuleFor(x => x.Msisdn).NotEmpty();
        RuleFor(x => x.ServiceCode).NotEmpty();
    }
}

public class ToggleSubscriberVasServiceHandler : IRequestHandler<ToggleSubscriberVasServiceRequest, ToggleSubscriberVasServiceResult>
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<SubscriberActiveService> _activeRepository;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVasProvisioningService _vasProvisioning;
    private readonly VasMsisdnLock _msisdnLock;
    private readonly IUserAuditService _audit;
    private readonly NumberSequenceService _numberSequenceService;

    public ToggleSubscriberVasServiceHandler(
        IQueryContext query,
        ICommandRepository<SubscriberActiveService> activeRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IUnitOfWork unitOfWork,
        IVasProvisioningService vasProvisioning,
        VasMsisdnLock msisdnLock,
        IUserAuditService audit,
        NumberSequenceService numberSequenceService)
    {
        _query = query;
        _activeRepository = activeRepository;
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
        _vasProvisioning = vasProvisioning;
        _msisdnLock = msisdnLock;
        _audit = audit;
        _numberSequenceService = numberSequenceService;
    }

    public Task<ToggleSubscriberVasServiceResult> Handle(
        ToggleSubscriberVasServiceRequest request,
        CancellationToken cancellationToken)
    {
        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.Msisdn)
            ?? throw new InvalidOperationException("Invalid MSISDN.");

        return _msisdnLock.RunExclusiveAsync(
            msisdn,
            () => HandleCoreAsync(request, msisdn, cancellationToken),
            cancellationToken);
    }

    private async Task<ToggleSubscriberVasServiceResult> HandleCoreAsync(
        ToggleSubscriberVasServiceRequest request,
        string msisdn,
        CancellationToken cancellationToken)
    {
        var subscription = await _query.TelecomSubscription
            .Include(s => s.MsisdnAsset)
            .Include(s => s.SubscriberProfile)
            .Include(s => s.SubscriptionTypeLookup)
            .FirstOrDefaultAsync(
                s => !s.IsDeleted && s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == msisdn,
                cancellationToken)
            ?? throw new InvalidOperationException("No subscription found for MSISDN.");

        var serviceCode = request.ServiceCode.Trim().ToUpperInvariant();
        var vas = await _query.TelecomValueAddedService
            .FirstOrDefaultAsync(
                x => !x.IsDeleted && x.IsActive && x.ServiceCode == serviceCode,
                cancellationToken)
            ?? throw new InvalidOperationException($"VAS service '{serviceCode}' not found or inactive.");

        var activate = request.Action == VasToggleAction.Activate;

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
                return new ToggleSubscriberVasServiceResult
                {
                    IsActive = true,
                    Msisdn = msisdn,
                    ServiceCode = serviceCode,
                };
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
                return new ToggleSubscriberVasServiceResult
                {
                    IsActive = false,
                    Msisdn = msisdn,
                    ServiceCode = serviceCode,
                };
            }
        }

        var correlationId = Guid.CreateVersion7().ToString();
        var (operationId, operationNumber) = await CreateVasOperationTrailAsync(
            subscription,
            vas,
            activate,
            request.ActorUserId,
            cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var provision = await _vasProvisioning.ProvisionVasAsync(
            new VasProvisionRequest(msisdn, vas.HlrCommandTemplate, activate, correlationId, operationId),
            cancellationToken);

        if (!provision.Success)
        {
            throw new BusinessRuleViolationException(provision.Message);
        }

        if (activate)
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
                existing.UpdatedById = request.ActorUserId;
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
                        CreatedById = request.ActorUserId,
                    },
                    cancellationToken);
            }

            if (IsPrepaid(subscription))
            {
                var profile = subscription.SubscriberProfile
                    ?? throw new InvalidOperationException("Subscriber profile not found.");
                profile.PrepaidBalance = (profile.PrepaidBalance ?? 0) - vas.MonthlyFee;
                if (profile.PrepaidBalance < 0)
                {
                    profile.PrepaidBalance = 0;
                }
            }
        }
        else
        {
            var row = await _query.SubscriberActiveService.FirstOrDefaultAsync(
                x => !x.IsDeleted
                    && x.TelecomSubscriptionId == subscription.Id
                    && x.TelecomValueAddedServiceId == vas.Id
                    && x.Status == SubscriberVasStatus.Active,
                cancellationToken);
            if (row != null)
            {
                row.Status = SubscriberVasStatus.Suspended;
                row.DeactivatedAtUtc = DateTime.UtcNow;
                row.UpdatedById = request.ActorUserId;
                row.UpdatedAtUtc = DateTime.UtcNow;
                _activeRepository.Update(row);
            }
        }

        await _unitOfWork.SaveAsync(cancellationToken);

        var actionAr = activate ? "تفعيل" : "تعطيل";
        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.ActorUserId ?? "",
                ActionType = UserAuditActionTypes.NetworkCommandExecuted,
                EntityType = "VAS",
                EntityId = subscription.Id,
                SummaryAr = $"الوكيل {actionAr} خدمة {serviceCode} للخط {msisdn}",
                Payload = new
                {
                    msisdn,
                    serviceCode,
                    activate,
                    provision.ExecutedCommand,
                    operationNumber,
                },
                IpAddress = request.IpAddress,
            },
            cancellationToken);

        return new ToggleSubscriberVasServiceResult
        {
            IsActive = activate,
            Msisdn = msisdn,
            ServiceCode = serviceCode,
            OperationNumber = operationNumber,
        };
    }

    private static bool IsPrepaid(Domain.Entities.TelecomSubscription subscription) =>
        string.Equals(
            subscription.SubscriptionTypeLookup?.Code,
            "PREPAID",
            StringComparison.OrdinalIgnoreCase);

    private static async Task ValidatePrepaidBalanceAsync(
        Domain.Entities.TelecomSubscription subscription,
        TelecomValueAddedService vas,
        CancellationToken cancellationToken)
    {
        if (!IsPrepaid(subscription))
        {
            return;
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

        await Task.CompletedTask;
    }

    private async Task<(string OperationId, string OperationNumber)> CreateVasOperationTrailAsync(
        Domain.Entities.TelecomSubscription subscription,
        TelecomValueAddedService vas,
        bool activate,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var (entityName, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.ServiceModification);
        var number = _numberSequenceService.GenerateNumber(entityName, prefix, "", useDate: false);

        var entity = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.ServiceModification,
            Number = number,
            CorrelationId = Guid.CreateVersion7().ToString(),
            Status = TelecomOperationStatus.Confirmed,
            DocumentStatus = TelecomDocumentStatus.Verified,
            SubscriberProfileId = subscription.SubscriberProfileId,
            MsisdnAssetId = subscription.MsisdnAssetId,
            Notes = $"{(activate ? "Activate" : "Deactivate")} VAS {vas.ServiceCode}",
            ConfirmedAtUtc = DateTime.UtcNow,
            CreatedById = actorUserId,
        };

        await _operationRepository.CreateAsync(entity, cancellationToken);
        return (entity.Id, number);
    }
}
