using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.LeadContactManager.Commands;

public class UpdateLeadContactResult
{
    public LeadContact? Data { get; set; }
}

public class UpdateLeadContactRequest : IRequest<UpdateLeadContactResult>
{
    public string? Id { get; init; }
    public string? LeadId { get; init; }
    public string? FullName { get; init; }
    public string? Description { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressState { get; init; }
    public string? AddressZipCode { get; init; }
    public string? AddressCountry { get; init; }
    public string? PhoneNumber { get; init; }
    public string? FaxNumber { get; init; }
    public string? MobileNumber { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? WhatsApp { get; init; }
    public string? LinkedIn { get; init; }
    public string? Facebook { get; init; }
    public string? Twitter { get; init; }
    public string? Instagram { get; init; }
    public string? AvatarName { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateLeadContactValidator : AbstractValidator<UpdateLeadContactRequest>
{
    public UpdateLeadContactValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.AddressStreet).NotEmpty();
        RuleFor(x => x.AddressCity).NotEmpty();
        RuleFor(x => x.AddressState).NotEmpty();
        RuleFor(x => x.MobileNumber).NotEmpty();
        RuleFor(x => x.Email).NotEmpty();
    }
}

public class UpdateLeadContactHandler : IRequestHandler<UpdateLeadContactRequest, UpdateLeadContactResult>
{
    private readonly ICommandRepository<LeadContact> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLeadContactHandler(
        ICommandRepository<LeadContact> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateLeadContactResult> Handle(UpdateLeadContactRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.LeadId = request.LeadId;
        entity.FullName = request.FullName;
        entity.Description = request.Description;
        entity.AddressStreet = request.AddressStreet;
        entity.AddressCity = request.AddressCity;
        entity.AddressState = request.AddressState;
        entity.AddressZipCode = request.AddressZipCode;
        entity.AddressCountry = request.AddressCountry;
        entity.PhoneNumber = request.PhoneNumber;
        entity.FaxNumber = request.FaxNumber;
        entity.MobileNumber = request.MobileNumber;
        entity.Email = request.Email;
        entity.Website = request.Website;
        entity.WhatsApp = request.WhatsApp;
        entity.LinkedIn = request.LinkedIn;
        entity.Facebook = request.Facebook;
        entity.Twitter = request.Twitter;
        entity.Instagram = request.Instagram;
        entity.AvatarName = request.AvatarName;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateLeadContactResult
        {
            Data = entity
        };
    }
}