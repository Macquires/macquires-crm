using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class EnsureSubscriberProfileForCustomerResult
{
    public string SubscriberProfileId { get; set; } = "";
    public bool Created { get; set; }
}

public class EnsureSubscriberProfileForCustomerRequest : IRequest<EnsureSubscriberProfileForCustomerResult>
{
    public string CustomerId { get; init; } = "";
    public string? CreatedById { get; init; }
}

public class EnsureSubscriberProfileForCustomerValidator : AbstractValidator<EnsureSubscriberProfileForCustomerRequest>
{
    public EnsureSubscriberProfileForCustomerValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}

public class EnsureSubscriberProfileForCustomerHandler
    : IRequestHandler<EnsureSubscriberProfileForCustomerRequest, EnsureSubscriberProfileForCustomerResult>
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EnsureSubscriberProfileForCustomerHandler(
        IQueryContext query,
        ICommandRepository<SubscriberProfile> profileRepository,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EnsureSubscriberProfileForCustomerResult> Handle(
        EnsureSubscriberProfileForCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = request.CustomerId.Trim();
        var customerExists = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(c => c.Id == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new BusinessRuleViolationException("المشترك (العميل) غير موجود.");
        }

        var existingId = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == customerId)
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(existingId))
        {
            return new EnsureSubscriberProfileForCustomerResult
            {
                SubscriberProfileId = existingId,
                Created = false,
            };
        }

        var profile = new SubscriberProfile
        {
            CustomerId = customerId,
            ServiceLineType = ServiceLineType.Mobile,
            LoyaltyPoints = 0,
            LoyaltyTier = "Bronze",
            CreatedById = request.CreatedById,
        };
        await _profileRepository.CreateAsync(profile, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EnsureSubscriberProfileForCustomerResult
        {
            SubscriberProfileId = profile.Id,
            Created = true,
        };
    }
}
