using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class RegisterSubscriberProfileForCustomerResult
{
    public SubscriberProfile? SubscriberProfile { get; set; }
    public TelecomSubscription? TelecomSubscription { get; set; }
}

public class RegisterSubscriberProfileForCustomerRequest : IRequest<RegisterSubscriberProfileForCustomerResult>, IRequireAnyPermission
{
    public string CustomerId { get; init; } = "";
    public string PrimaryMsisdn { get; init; } = "";
    public string PrimarySubscriptionTypeId { get; init; } = "";
    public string? SimInventoryId { get; init; }
    public string? ProductOfferingId { get; init; }
    public ServiceLineType ServiceLineType { get; init; } = ServiceLineType.Mobile;

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.CustomerProvisioningAny;
}

public class RegisterSubscriberProfileForCustomerValidator : AbstractValidator<RegisterSubscriberProfileForCustomerRequest>
{
    public RegisterSubscriberProfileForCustomerValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PrimarySubscriptionTypeId).NotEmpty();
        RuleFor(x => x.PrimaryMsisdn)
            .NotEmpty()
            .Must(v => TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(v) != null)
            .WithMessage("صيغة رقم الموبايل غير صالحة. استخدم 09xxxxxxxx أو +9639xxxxxxxx.");
    }
}

public class RegisterSubscriberProfileForCustomerHandler
    : IRequestHandler<RegisterSubscriberProfileForCustomerRequest, RegisterSubscriberProfileForCustomerResult>
{
    private const string DuplicateMsisdnAr = "عذراً، هذا الرقم (MSISDN) مرتبط حالياً بمشترك أو خط آخر.";
    private const string CustomerNotFoundAr = "المشترك (العميل) غير موجود.";

    private readonly IQueryContext _query;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISubscriptionBindingExecutor _bindingExecutor;
    private readonly IOperatorContext _operator;

    public RegisterSubscriberProfileForCustomerHandler(
        IQueryContext query,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        IUnitOfWork unitOfWork,
        ISubscriptionBindingExecutor bindingExecutor,
        IOperatorContext operatorContext)
    {
        _query = query;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
        _bindingExecutor = bindingExecutor;
        _operator = operatorContext;
    }

    public async Task<RegisterSubscriberProfileForCustomerResult> Handle(
        RegisterSubscriberProfileForCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);
        var branchId = OperatorActor.ResolveBranchId(_operator);
        var customerId = request.CustomerId.Trim();
        var customerExists = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(c => c.Id == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new BusinessRuleViolationException(CustomerNotFoundAr);
        }

        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.PrimaryMsisdn)!;
        var taken = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(m => m.Msisdn == msisdn && m.PoolStatus != MsisdnPoolStatus.Available, cancellationToken);
        if (taken)
        {
            throw new BusinessRuleViolationException(DuplicateMsisdnAr);
        }

        var typeLookup = await _query.TelecomSubscriptionTypeLookup.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(x => x.Id == request.PrimarySubscriptionTypeId.Trim() && x.IsActive, cancellationToken);
        if (typeLookup == null)
        {
            throw new BusinessRuleViolationException("نوع الخط غير صالح أو غير مفعّل.");
        }

        var productId = await _query.Product.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Physical == false)
            .OrderBy(p => p.Name)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(productId))
        {
            throw new BusinessRuleViolationException("لا يوجد منتج خدمة في الكتالوج.");
        }

        var profileIds = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(sp => sp.CustomerId == customerId).Select(sp => sp.Id).ToListAsync(cancellationToken);
        var partyHasLine = profileIds.Count > 0 && await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(s => profileIds.Contains(s.SubscriberProfileId) && s.MsisdnAssetId != null, cancellationToken);

        var profile = new SubscriberProfile
        {
            CustomerId = customerId,
            ServiceLineType = request.ServiceLineType,
            LoyaltyPoints = 0,
            LoyaltyTier = "Bronze",
            CreatedById = actorUserId,
            BranchId = branchId,
        };
        await _profileRepository.CreateAsync(profile, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var asset = await _query.MsisdnAsset.IsDeletedEqualTo()
            .FirstOrDefaultAsync(m => m.Msisdn == msisdn, cancellationToken);

        if (asset == null)
        {
            asset = new MsisdnAsset
            {
                Msisdn = msisdn,
                CountryCode = "963",
                CreatedById = actorUserId,
                BranchId = branchId,
            };
            await _msisdnRepository.CreateAsync(asset, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.SimInventoryId))
        {
            var bindResult = await _bindingExecutor.ExecuteAsync(
                new BindSubscriptionCommand(
                    customerId,
                    profile.Id,
                    asset.Id,
                    request.SimInventoryId.Trim(),
                    request.ProductOfferingId ?? string.Empty,
                    profile.Id,
                    Guid.CreateVersion7().ToString()),
                typeLookup.Id,
                actorUserId,
                requireStrictReservation: false,
                cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
            return new RegisterSubscriberProfileForCustomerResult
            {
                SubscriberProfile = bindResult.SubscriberProfile,
                TelecomSubscription = bindResult.TelecomSubscription
            };
        }

        asset.SubscriberProfileId = profile.Id;
        asset.ProductId = productId;
        asset.TransitionTo(MsisdnPoolStatus.Active);
        profile.Activate();
        _msisdnRepository.Update(asset);
        _profileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        var subscription = new TelecomSubscription
        {
            SubscriberProfileId = profile.Id,
            MsisdnAssetId = asset.Id,
            ProductId = productId,
            SubscriptionTypeId = typeLookup.Id,
            DocumentStatus = TelecomDocumentStatus.Missing,
            IsPrimaryLine = !partyHasLine,
            CreatedById = actorUserId,
        };
        await _subscriptionRepository.CreateAsync(subscription, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new RegisterSubscriberProfileForCustomerResult
        {
            SubscriberProfile = profile,
            TelecomSubscription = subscription
        };
    }
}
