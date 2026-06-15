using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Commands;

public class UpdateCustomerResult
{
    public Customer? Data { get; set; }
}

public class UpdateCustomerRequest : IRequest<UpdateCustomerResult>, IRequireAnyPermission
{
    public string? Id { get; init; }
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

    [System.Text.Json.Serialization.JsonPropertyName("subscriberType")]
    public CustomerKind? CustomerKind { get; set; }
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? CommercialRegistration { get; set; }
    public string? TaxNumber { get; set; }
    public string? AuthorizedSignatory { get; set; }

    public IReadOnlyList<string> PermissionKeys => CustomerPermissionSets.ManageAny;
}

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.ZipCode).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.EmailAddress).NotEmpty();
        RuleFor(x => x.CustomerGroupId).NotEmpty();
        RuleFor(x => x.CustomerCategoryId).NotEmpty();
    }
}

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerRequest, UpdateCustomerResult>
{
    private readonly ICommandRepository<Customer> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _query;
    private readonly IFieldEncryptionService _encryption;
    private readonly IUserAuditService _audit;
    private readonly IOperatorContext _operator;

    public UpdateCustomerHandler(
        ICommandRepository<Customer> repository,
        IUnitOfWork unitOfWork,
        IQueryContext query,
        IFieldEncryptionService encryption,
        IUserAuditService audit,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _query = query;
        _encryption = encryption;
        _audit = audit;
        _operator = operatorContext;
    }

    public async Task<UpdateCustomerResult> Handle(UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);
        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        var actorUserId = OperatorActor.RequireUserId(_operator);

        entity.UpdatedById = actorUserId;
        entity.SetDisplayName(request.Name!);
        entity.SetDescription(request.Description);
        entity.UpdateAddress(new PostalAddress(request.Street, request.City, request.State, request.ZipCode, request.Country));
        entity.UpdateContact(request.EmailAddress, request.PhoneNumber, request.FaxNumber, request.Website);
        entity.UpdateSocial(request.WhatsApp, request.LinkedIn, request.Facebook, request.Instagram, request.TwitterX, request.TikTok);
        entity.SetCustomerGroup(request.CustomerGroupId, request.CustomerCategoryId);

        if (entity is IndividualCustomer individual)
        {
            if (!string.IsNullOrWhiteSpace(request.NationalId))
            {
                var nid = request.NationalId.Trim();
                var nidHash = _encryption.ComputeSearchHash(nid);
                var dup = await _query.Customer.OfType<IndividualCustomer>()
                    .AnyAsync(c => c.NationalIdSearchHash == nidHash && c.Id != entity.Id, cancellationToken);
                if (dup)
                {
                    throw new BusinessRuleViolationException("الرقم الوطني مسجّل مسبقاً.");
                }
                individual.UpdateIdentity(
                    nid,
                    request.DateOfBirth.HasValue ? DateOnly.FromDateTime(request.DateOfBirth.Value) : individual.DateOfBirth,
                    individual.Nationality,
                    individual.Gender,
                    individual.Occupation);
                individual.SetNationalIdSearchHash(nidHash);
            }
        }
        else if (entity is CorporateCustomer corporate && !string.IsNullOrWhiteSpace(request.CommercialRegistration))
        {
            var reg = request.CommercialRegistration.Trim();
            var dup = await _query.Customer.OfType<CorporateCustomer>()
                .AnyAsync(c => c.CommercialRegistryNumber == reg && c.Id != entity.Id, cancellationToken);
            if (dup)
            {
                throw new BusinessRuleViolationException("رقم السجل التجاري مسجّل مسبقاً.");
            }
            corporate.UpdateCorporateIdentity(reg, request.TaxNumber, request.AuthorizedSignatory, corporate.LegalStatus);
        }

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId,
                ActionType = UserAuditActionTypes.CustomerUpdated,
                EntityType = nameof(Customer),
                EntityId = entity.Id,
                SummaryAr = $"تحديث مشترك: {entity.DisplayName}",
            },
            cancellationToken);

        return new UpdateCustomerResult { Data = entity };
    }
}
