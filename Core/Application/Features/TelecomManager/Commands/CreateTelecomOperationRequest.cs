using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Settings;
using Application.Common.Telecom;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public static class TelecomNumberSequence
{
    public static (string EntityName, string Prefix) ForKind(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.Migration => ("TelecomOp_Migration", "MGR-"),
        TelecomOperationKind.TakeOver => ("TelecomOp_TakeOver", "TKO-"),
        TelecomOperationKind.NewActivation => ("TelecomOp_Activation", "ACT-"),
        TelecomOperationKind.SimSwap => ("TelecomOp_SimSwap", "SIM-"),
        TelecomOperationKind.ServiceModification => ("TelecomOp_Vas", "VAS-"),
        TelecomOperationKind.NumberPortability => ("TelecomOp_Mnp", "MNP-"),
        _ => ("TelecomOp_Generic", "TEL-")
    };
}

public class CreateTelecomOperationRequestResult
{
    public TelecomOperationRequest? Data { get; set; }
}
public class CreateTelecomOperationRequest : IRequest<CreateTelecomOperationRequestResult>
{
    public TelecomOperationKind Kind { get; init; }
    public string SubscriberProfileId { get; init; } = null!;
    public string? SecondarySubscriberProfileId { get; init; }
    public string? MsisdnAssetId { get; init; }

    /// <summary>Commercial catalog selection (preferred). Resolved to technical <see cref="Product"/> id via the linked offering.</summary>
    public string? ProductOfferingId { get; init; }

    /// <summary>Legacy technical product id (optional when <see cref="ProductOfferingId"/> is set).</summary>
    public string? ProductId { get; init; }
    public string? Notes { get; init; }
    public string? TargetOfferName { get; init; }
    public string? SimInventoryId { get; init; }

    /// <summary>Resolves <see cref="SimInventoryId"/> when set (available SIM in pool).</summary>
    public string? SimIccid { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateTelecomOperationRequestValidator : AbstractValidator<CreateTelecomOperationRequest>
{
    public CreateTelecomOperationRequestValidator()
    {
        RuleFor(x => x.SubscriberProfileId).NotEmpty();
        RuleFor(x => x.SecondarySubscriberProfileId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TakeOver);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.TargetOfferName).MaximumLength(200);
        RuleFor(x => x.ProductOfferingId).MaximumLength(450);
        RuleFor(x => x.SimIccid)
            .Must(iccid => IccidValidator.TryValidate(iccid, out _, out _))
            .When(x => x.Kind == TelecomOperationKind.NewActivation && !string.IsNullOrWhiteSpace(x.SimIccid))
            .WithMessage("ICCID غير صالح (19–20 رقماً).");
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.NewActivation);
    }
}

public class CreateTelecomOperationRequestHandler : IRequestHandler<CreateTelecomOperationRequest, CreateTelecomOperationRequestResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IQueryContext _queryContext;
    private readonly IBillingSystemIntegration _billing;
    private readonly ITechnicalTicketQueueIngestionService _ticketQueue;
    private readonly IGlobalSettingsProvider _globalSettings;

    public CreateTelecomOperationRequestHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IQueryContext queryContext,
        IBillingSystemIntegration billing,
        ITechnicalTicketQueueIngestionService ticketQueue,
        IGlobalSettingsProvider globalSettings)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _queryContext = queryContext;
        _billing = billing;
        _ticketQueue = ticketQueue;
        _globalSettings = globalSettings;
    }

    public async Task<CreateTelecomOperationRequestResult> Handle(
        CreateTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Kind == TelecomOperationKind.TakeOver)
        {
            await ValidateTakeOverDebtAsync(request, cancellationToken);
        }

        if (request.Kind == TelecomOperationKind.NewActivation)
        {
            await ValidateNewActivationLineCapAsync(request, cancellationToken);
        }

        var offeringIdInput = (request.ProductOfferingId ?? string.Empty).Trim();
        var productIdInput = (request.ProductId ?? string.Empty).Trim();

        string? resolvedOfferingId = null;
        string? resolvedProductId = null;

        if (!string.IsNullOrEmpty(offeringIdInput))
        {
            var offeringEntity = await _queryContext.ProductOffering
                .AsNoTracking()
                .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == offeringIdInput, cancellationToken)
                ?? throw new BusinessRuleViolationException("العرض التجاري المختار غير موجود.");

            var linked = (offeringEntity.ProductId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(linked))
            {
                throw new BusinessRuleViolationException(
                    "هذا العرض غير مربوط بمنتج تقني للتفعيل. يرجى ربط منتج (CBS) من كتالوج الباقات.");
            }

            resolvedOfferingId = offeringEntity.Id;
            resolvedProductId = linked;
        }
        else if (!string.IsNullOrEmpty(productIdInput))
        {
            resolvedProductId = productIdInput;
        }

        if (request.Kind is TelecomOperationKind.Migration or TelecomOperationKind.NewActivation
            && !string.IsNullOrEmpty(resolvedProductId))
        {
            await ValidateCatalogSelectionAgainstSubscriptionAsync(
                request,
                resolvedProductId,
                resolvedOfferingId,
                cancellationToken);
        }

        var simInventoryId = (request.SimInventoryId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(simInventoryId) && !string.IsNullOrWhiteSpace(request.SimIccid))
        {
            var iccid = request.SimIccid.Trim();
            var sim = await _queryContext.SimInventory
                .AsNoTracking()
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Iccid == iccid, cancellationToken)
                ?? throw new BusinessRuleViolationException("الشريحة (ICCID) غير موجودة في المستودع.");
            simInventoryId = sim.Id;
        }

        var (entityName, prefix) = TelecomNumberSequence.ForKind(request.Kind);
        var number = _numberSequenceService.GenerateNumber(entityName, prefix, "", useDate: false);

        var notes = BuildNotes(request);

        var entity = new TelecomOperationRequest
        {
            Kind = request.Kind,
            Number = number,
            CorrelationId = Guid.CreateVersion7().ToString(),
            Status = TelecomOperationStatus.Draft,
            DocumentStatus = TelecomDocumentStatus.Missing,
            SubscriberProfileId = request.SubscriberProfileId,
            SecondarySubscriberProfileId = request.SecondarySubscriberProfileId,
            MsisdnAssetId = request.MsisdnAssetId,
            SimInventoryId = string.IsNullOrEmpty(simInventoryId) ? null : simInventoryId,
            ProductId = resolvedProductId,
            ProductOfferingId = resolvedOfferingId,
            Notes = notes,
            TargetOfferName = request.TargetOfferName,
            CreatedById = request.CreatedById
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _ticketQueue.EnqueueFromTelecomOperationAsync(entity, request.CreatedById, cancellationToken);

        return new CreateTelecomOperationRequestResult { Data = entity };
    }

    private async Task ValidateCatalogSelectionAgainstSubscriptionAsync(
        CreateTelecomOperationRequest request,
        string resolvedProductId,
        string? resolvedOfferingId,
        CancellationToken cancellationToken)
    {
        var subs = await _queryContext.TelecomSubscription
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == request.SubscriberProfileId)
            .Include(s => s.SubscriptionTypeLookup)
            .ToListAsync(cancellationToken);

        if (subs.Count == 0)
        {
            throw new BusinessRuleViolationException(
                "لا يوجد اشتراك مرتبط بملف المشترك؛ لا يمكن التحقق من نوع الخط لتطبيق قواعد توافق الباقة.");
        }

        var msisdnId = (request.MsisdnAssetId ?? string.Empty).Trim();
        TelecomSubscription? chosen = null;
        if (request.Kind == TelecomOperationKind.NewActivation && !string.IsNullOrEmpty(msisdnId))
        {
            var poolAsset = await _queryContext.MsisdnAsset
                .AsNoTracking()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == msisdnId, cancellationToken);
            if (poolAsset?.PoolStatus == MsisdnPoolStatus.Available)
            {
                chosen = subs.FirstOrDefault(s => s.IsPrimaryLine) ?? subs.First();
            }
        }

        if (chosen == null && !string.IsNullOrEmpty(msisdnId))
        {
            chosen = subs.FirstOrDefault(s => s.MsisdnAssetId == msisdnId);
        }

        chosen ??= subs.FirstOrDefault(s => s.IsPrimaryLine) ?? subs.First();

        var lineTypeId = chosen.SubscriptionTypeId;

        var product = await _queryContext.Product
            .AsNoTracking()
            .FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == resolvedProductId, cancellationToken)
            ?? throw new BusinessRuleViolationException("المنتج التقني المرتبط غير موجود.");

        if (!string.IsNullOrEmpty(resolvedOfferingId))
        {
            var offering = await _queryContext.ProductOffering
                .AsNoTracking()
                .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == resolvedOfferingId, cancellationToken);

            if (offering != null
                && !string.IsNullOrEmpty(offering.CompatibleSubscriptionTypeId)
                && offering.CompatibleSubscriptionTypeId != lineTypeId)
            {
                throw new BusinessRuleViolationException(
                    $"نوع الخط الحالي ({chosen.SubscriptionTypeLookup?.NameAr ?? "غير معروف"}) غير متوافق مع العرض التجاري المختار.");
            }
        }

        if (!string.IsNullOrEmpty(product.CompatibleSubscriptionTypeId)
            && product.CompatibleSubscriptionTypeId != lineTypeId)
        {
            throw new BusinessRuleViolationException(
                $"نوع الخط الأساسي ({chosen.SubscriptionTypeLookup?.NameAr ?? "غير معروف"}) غير متوافق مع الباقة المطلوبة.");
        }
    }

    private static string? BuildNotes(CreateTelecomOperationRequest request)
    {
        var existing = (request.Notes ?? string.Empty).Trim();
        if (existing.StartsWith("Customer360|", StringComparison.OrdinalIgnoreCase))
        {
            return existing.Length > 0 ? existing : null;
        }

        string? msisdnLabel = null;
        if (!string.IsNullOrWhiteSpace(request.MsisdnAssetId))
        {
            msisdnLabel = request.MsisdnAssetId.Trim();
        }

        var auditPrefix = $"Customer360|{request.Kind}|{msisdnLabel ?? "—"}";
        if (string.IsNullOrEmpty(existing))
        {
            return auditPrefix;
        }

        return $"{auditPrefix} | {existing}";
    }

    private async Task ValidateNewActivationLineCapAsync(
        CreateTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var maxLines = await _globalSettings.GetIntAsync(
            GlobalSettingKeys.TelecomMaxActiveLinesPerIndividual,
            defaultValue: 5,
            min: 1,
            max: 20,
            cancellationToken);

        var customerId = await _queryContext.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == request.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(customerId))
        {
            return;
        }

        var isIndividual = await _queryContext.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Id == customerId)
            .Select(c => c.CustomerKind)
            .FirstOrDefaultAsync(cancellationToken) == CustomerKind.Individual;

        if (!isIndividual)
        {
            return;
        }

        var activeLineCount = await (
            from s in _queryContext.TelecomSubscription.AsNoTracking()
            join m in _queryContext.MsisdnAsset.AsNoTracking() on s.MsisdnAssetId equals m.Id
            join p in _queryContext.SubscriberProfile.AsNoTracking() on s.SubscriberProfileId equals p.Id
            where !s.IsDeleted && !m.IsDeleted && p.CustomerId == customerId
                  && m.PoolStatus == MsisdnPoolStatus.Active
            select s.Id
        ).CountAsync(cancellationToken);

        if (activeLineCount >= maxLines)
        {
            throw new BusinessRuleViolationException(
                $"تجاوز الحد التنظيمي للخطوط النشطة ({maxLines}) لهذا العميل.");
        }
    }

    private async Task ValidateTakeOverDebtAsync(CreateTelecomOperationRequest request, CancellationToken cancellationToken)
    {
        var msisdnId = (request.MsisdnAssetId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(msisdnId))
        {
            return;
        }

        var asset = await _queryContext.MsisdnAsset
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == msisdnId && !m.IsDeleted, cancellationToken);

        if (asset != null && !string.IsNullOrEmpty(asset.Msisdn))
        {
            if (asset.PoolStatus != Domain.Enums.MsisdnPoolStatus.Active)
            {
                throw new BusinessRuleViolationException($"لا يمكن نقل ملكية الرقم {asset.Msisdn} لأن حالته الحالية هي {asset.PoolStatus} بدلاً من مفعل.");
            }

            var outstandingBalance = await _billing.GetOutstandingBalanceAsync(asset.Msisdn, cancellationToken);
            if (outstandingBalance < 0)
            {
                throw new BusinessRuleViolationException($"لا يمكن نقل ملكية الرقم {asset.Msisdn}. يوجد ذمم مالية معلقة بقيمة {-outstandingBalance} ل.س.");
            }
        }
    }
}
