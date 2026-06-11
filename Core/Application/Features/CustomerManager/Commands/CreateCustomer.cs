using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Commands;

public class CreateCustomerResult
{
    public Customer? Data { get; set; }
}

public class CreateCustomerRequest : IRequest<CreateCustomerResult>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Website { get; set; }
    public string? WhatsApp { get; set; }
    public string? LinkedIn { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? TwitterX { get; set; }
    public string? TikTok { get; set; }
    public string? CustomerGroupId { get; set; }
    public string? CustomerCategoryId { get; set; }
    public string? CreatedById { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("subscriberType")]
    public CustomerKind? CustomerKind { get; set; }
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? CommercialRegistration { get; set; }
    public string? TaxNumber { get; set; }
    public string? AuthorizedSignatory { get; set; }
    public string? Nationality { get; set; }
    public Gender? Gender { get; set; }
    public string? Occupation { get; set; }

    /// <summary>Telecom Hub / POS onboarding — minimal fields; defaults applied before validation.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("posQuickRegister")]
    public bool PosQuickRegister { get; set; }
}

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty();

        When(x => x.PosQuickRegister, () =>
        {
            When(x => x.CustomerKind != CustomerKind.Corporate, () =>
            {
                RuleFor(x => x.NationalId)
                    .NotEmpty().WithMessage("الرقم الوطني مطلوب للأفراد.")
                    .Length(10).WithMessage("الرقم الوطني يجب أن يتكون من 10 خانات.");
            });

            When(x => x.CustomerKind == CustomerKind.Corporate, () =>
            {
                RuleFor(x => x.CommercialRegistration)
                    .NotEmpty().WithMessage("رقم السجل التجاري مطلوب للشركات.")
                    .Matches(@"^[A-Za-z0-9\-\s]{4,20}$").WithMessage("صيغة السجل التجاري غير صالحة.");
            });
        });

        When(x => !x.PosQuickRegister, () =>
        {
            RuleFor(x => x.Street).NotEmpty();
            RuleFor(x => x.City).NotEmpty();
            RuleFor(x => x.State).NotEmpty();
            RuleFor(x => x.ZipCode).NotEmpty();
            RuleFor(x => x.CustomerGroupId).NotEmpty();
            RuleFor(x => x.CustomerCategoryId).NotEmpty();

            When(x => x.CustomerKind != CustomerKind.Corporate, () =>
            {
                RuleFor(x => x.NationalId)
                    .NotEmpty().WithMessage("الرقم الوطني مطلوب للأفراد.")
                    .Length(10).WithMessage("الرقم الوطني يجب أن يتكون من 10 خانات.");
            });

            When(x => x.CustomerKind == CustomerKind.Corporate, () =>
            {
                RuleFor(x => x.CommercialRegistration)
                    .NotEmpty().WithMessage("رقم السجل التجاري مطلوب للشركات.")
                    .Matches(@"^[A-Za-z0-9\-\s]{4,20}$").WithMessage("صيغة السجل التجاري غير صالحة.");
            });
        });
    }
}

public class CreateCustomerHandler : IRequestHandler<CreateCustomerRequest, CreateCustomerResult>
{
    private readonly ICommandRepository<Customer> _repository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IQueryContext _query;
    private readonly IFieldEncryptionService _encryption;
    private readonly IUserAuditService _audit;

    public CreateCustomerHandler(
        ICommandRepository<Customer> repository,
        ICommandRepository<SubscriberProfile> profileRepository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IQueryContext query,
        IFieldEncryptionService encryption,
        IUserAuditService audit)
    {
        _repository = repository;
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _query = query;
        _encryption = encryption;
        _audit = audit;
    }

    public async Task<CreateCustomerResult> Handle(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var address = new PostalAddress(request.Street, request.City, request.State, request.ZipCode, request.Country);
        var accountNumber = _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST");
        var kind = request.CustomerKind ?? CustomerKind.Individual;

        if (kind == CustomerKind.Corporate)
        {
            var reg = (request.CommercialRegistration ?? "").Trim();
            var dup = await _query.Customer
                .OfType<CorporateCustomer>()
                .AnyAsync(c => c.CommercialRegistryNumber == reg, cancellationToken);
            if (dup)
            {
                throw new BusinessRuleViolationException("رقم السجل التجاري مسجّل مسبقاً.");
            }

            var entity = CorporateCustomer.Create(
                request.Name!,
                accountNumber,
                reg,
                address,
                request.EmailAddress,
                request.PhoneNumber,
                request.CustomerGroupId,
                request.CustomerCategoryId,
                request.TaxNumber,
                request.AuthorizedSignatory,
                CompanyLegalStatus.Unknown,
                request.Description);
            entity.CreatedById = request.CreatedById;
            entity.UpdateContact(request.EmailAddress, request.PhoneNumber, request.FaxNumber, request.Website);
            entity.UpdateSocial(request.WhatsApp, request.LinkedIn, request.Facebook, request.Instagram, request.TwitterX, request.TikTok);
            await _repository.CreateAsync(entity, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
            await CreateDefaultProfileAsync(entity.Id, request, cancellationToken);
            await LogCustomerCreatedAsync(entity, request, cancellationToken);
            return new CreateCustomerResult { Data = entity };
        }

        var nationalId = (request.NationalId ?? "").Trim();
        var nationalIdHash = _encryption.ComputeSearchHash(nationalId);
        var dupNat = await _query.Customer
            .OfType<IndividualCustomer>()
            .AnyAsync(c => c.NationalIdSearchHash == nationalIdHash, cancellationToken);
        if (dupNat)
        {
            throw new BusinessRuleViolationException("الرقم الوطني مسجّل مسبقاً.");
        }

        var individual = IndividualCustomer.Create(
            request.Name!,
            accountNumber,
            nationalId,
            address,
            request.EmailAddress,
            request.PhoneNumber,
            request.CustomerGroupId,
            request.CustomerCategoryId,
            request.DateOfBirth.HasValue ? DateOnly.FromDateTime(request.DateOfBirth.Value) : null,
            string.IsNullOrWhiteSpace(request.Nationality) ? null : request.Nationality.Trim(),
            request.Gender ?? Gender.Unknown,
            string.IsNullOrWhiteSpace(request.Occupation) ? null : request.Occupation.Trim(),
            request.Description);
        individual.CreatedById = request.CreatedById;
        individual.SetNationalIdSearchHash(nationalIdHash);
        individual.UpdateContact(request.EmailAddress, request.PhoneNumber, request.FaxNumber, request.Website);
        individual.UpdateSocial(request.WhatsApp, request.LinkedIn, request.Facebook, request.Instagram, request.TwitterX, request.TikTok);
        await _repository.CreateAsync(individual, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        await CreateDefaultProfileAsync(individual.Id, request, cancellationToken);
        await LogCustomerCreatedAsync(individual, request, cancellationToken);
        return new CreateCustomerResult { Data = individual };
    }

    private Task LogCustomerCreatedAsync(Customer entity, CreateCustomerRequest request, CancellationToken ct) =>
        _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.CreatedById ?? "system",
                ActionType = UserAuditActionTypes.CustomerCreated,
                EntityType = nameof(Customer),
                EntityId = entity.Id,
                SummaryAr = $"إنشاء مشترك: {entity.DisplayName}",
                Payload = new { entity.AccountNumber, request.PhoneNumber },
            },
            ct);

    private async Task CreateDefaultProfileAsync(string customerId, CreateCustomerRequest request, CancellationToken ct)
    {
        var profile = new SubscriberProfile
        {
            CustomerId = customerId,
            ServiceLineType = ServiceLineType.Mobile,
            OperationalStatus = SubscriberOperationalStatus.Pending,
            LoyaltyPoints = 0,
            LoyaltyTier = "Bronze",
            CreatedById = request.CreatedById
        };
        await _profileRepository.CreateAsync(profile, ct);
        await _unitOfWork.SaveAsync(ct);
    }
}
