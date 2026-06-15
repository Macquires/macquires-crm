using Application.Common.Repositories;
using Application.Common.CQS.Queries;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.ProductCatalogManager.Commands;

// ─── Result ───
public class UpdateProductOfferingResult
{
    public ProductOffering? Data { get; set; }
}

// ─── Request ───
public class UpdateProductOfferingRequest : IRequest<UpdateProductOfferingResult>, IRequireAnyPermission
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? NameEn { get; init; }
    public string? Description { get; init; }
    public string? Code { get; init; }
    public string? CompatibleSubscriptionTypeId { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidToUtc { get; init; }
    public int SortOrder { get; init; }

    public string? EligibilityRules { get; init; }
    public string? AssetCompatibility { get; init; }
    public string? BillingCycle { get; init; }
    public string? TaxCategory { get; init; }
    public string? ServiceIdSocCode { get; init; }
    public double? SpeedQuotaLimitGb { get; init; }
    public int? VoiceMinutesLimit { get; init; }
    public string? ThrottlingPolicy { get; init; }
    public string? IconClass { get; init; }
    public string? BadgeColor { get; init; }
    public string? ShortDescription { get; init; }

    public string? ProductId { get; init; }

    /// <summary>Inline components to update/replace for the offering (optional).</summary>
    public List<CreateProductOfferingComponentDto>? Components { get; init; }

    /// <summary>Inline price plans to update/replace for the offering (optional).</summary>
    public List<CreatePricePlanDto>? PricePlans { get; init; }

    public IReadOnlyList<string> PermissionKeys => ProductCatalogPermissionSets.ManageAny;
}

// ─── Validator ───
public class UpdateProductOfferingValidator : AbstractValidator<UpdateProductOfferingRequest>
{
    public UpdateProductOfferingValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CompatibleSubscriptionTypeId)
            .NotEmpty()
            .WithMessage("يجب تحديد نوع الخط المتوافق مع الباقة (Prepaid/Postpaid/Hybrid).");

        RuleForEach(x => x.Components).ChildRules(comp =>
        {
            comp.RuleFor(c => c.Label).MaximumLength(255);
            comp.RuleFor(c => c.QuotaUnit).MaximumLength(50);
        });
        RuleForEach(x => x.PricePlans).ChildRules(pp =>
        {
            pp.RuleFor(p => p.Price).GreaterThanOrEqualTo(0);
            pp.RuleFor(p => p.CurrencyCode).NotEmpty().MaximumLength(50);
        });
        RuleFor(x => x.ProductId).MaximumLength(450);
    }
}

// ─── Handler ───
public class UpdateProductOfferingHandler : IRequestHandler<UpdateProductOfferingRequest, UpdateProductOfferingResult>
{
    private readonly ICommandRepository<ProductOffering> _repository;
    private readonly ICommandRepository<ProductOfferingComponent> _componentRepository;
    private readonly ICommandRepository<PricePlan> _pricePlanRepository;
    private readonly IQueryContext _queryContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public UpdateProductOfferingHandler(
        ICommandRepository<ProductOffering> repository,
        ICommandRepository<ProductOfferingComponent> componentRepository,
        ICommandRepository<PricePlan> pricePlanRepository,
        IQueryContext queryContext,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _componentRepository = componentRepository;
        _pricePlanRepository = pricePlanRepository;
        _queryContext = queryContext;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<UpdateProductOfferingResult> Handle(UpdateProductOfferingRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);
        var entity = await _repository.GetAsync(request.Id!, cancellationToken)
            ?? throw new InvalidOperationException("ProductOffering not found.");

        entity.Name = request.Name!;
        entity.NameEn = request.NameEn;
        entity.Description = request.Description;
        entity.Code = request.Code!;
        entity.CompatibleSubscriptionTypeId = request.CompatibleSubscriptionTypeId;
        entity.IsActive = request.IsActive;
        entity.ValidFromUtc = request.ValidFromUtc;
        entity.ValidToUtc = request.ValidToUtc;
        entity.SortOrder = request.SortOrder;
        entity.UpdatedById = actorUserId;

        entity.EligibilityRules = request.EligibilityRules;
        entity.AssetCompatibility = request.AssetCompatibility;
        entity.BillingCycle = request.BillingCycle;
        entity.TaxCategory = request.TaxCategory;
        entity.ServiceIdSocCode = request.ServiceIdSocCode;
        entity.SpeedQuotaLimitGb = request.SpeedQuotaLimitGb;
        entity.VoiceMinutesLimit = request.VoiceMinutesLimit;
        entity.ThrottlingPolicy = request.ThrottlingPolicy;
        entity.IconClass = request.IconClass;
        entity.BadgeColor = request.BadgeColor;
        entity.ShortDescription = request.ShortDescription;
        entity.ProductId = string.IsNullOrWhiteSpace(request.ProductId) ? null : request.ProductId.Trim();

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        // Delete existing components and replace them if new ones are provided
        if (request.Components != null)
        {
            var oldComponents = await _queryContext.ProductOfferingComponent
                .Where(x => x.ProductOfferingId == entity.Id)
                .ToListAsync(cancellationToken);

            foreach (var oldComp in oldComponents)
            {
                _componentRepository.Delete(oldComp);
            }
            await _unitOfWork.SaveAsync(cancellationToken);

            foreach (var comp in request.Components)
            {
                var component = new ProductOfferingComponent
                {
                    CreatedById = actorUserId,
                    ProductOfferingId = entity.Id,
                    ComponentType = comp.ComponentType,
                    Label = comp.Label,
                    Quota = comp.Quota,
                    QuotaUnit = comp.QuotaUnit,
                    IsUnlimited = comp.IsUnlimited,
                    SortOrder = comp.SortOrder
                };
                await _componentRepository.CreateAsync(component, cancellationToken);
            }
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        // Delete existing price plans and replace them if new ones are provided
        if (request.PricePlans != null)
        {
            var oldPlans = await _queryContext.PricePlan
                .Where(x => x.ProductOfferingId == entity.Id)
                .ToListAsync(cancellationToken);

            foreach (var oldPlan in oldPlans)
            {
                _pricePlanRepository.Delete(oldPlan);
            }
            await _unitOfWork.SaveAsync(cancellationToken);

            foreach (var pp in request.PricePlans)
            {
                var pricePlan = new PricePlan
                {
                    CreatedById = actorUserId,
                    ProductOfferingId = entity.Id,
                    PlanType = pp.PlanType,
                    Price = pp.Price,
                    CurrencyCode = pp.CurrencyCode,
                    ActivationFee = pp.ActivationFee,
                    ValidityDays = pp.ValidityDays,
                    IsDefault = pp.IsDefault
                };
                await _pricePlanRepository.CreateAsync(pricePlan, cancellationToken);
            }
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new UpdateProductOfferingResult { Data = entity };
    }
}
