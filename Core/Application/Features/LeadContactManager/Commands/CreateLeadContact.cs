using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.LeadContactManager.Commands;

public class CreateLeadContactResult
{
    public LeadContact? Data { get; set; }
}

public class CreateLeadContactRequest : IRequest<CreateLeadContactResult>
{
    public string? LeadId { get; init; }
    public string? FullName { get; init; }
    public string? Description { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressState { get; init; }
    public string? MobileNumber { get; init; }
    public string? Email { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateLeadContactValidator : AbstractValidator<CreateLeadContactRequest>
{
    public CreateLeadContactValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.AddressStreet).NotEmpty();
        RuleFor(x => x.AddressCity).NotEmpty();
        RuleFor(x => x.AddressState).NotEmpty();
        RuleFor(x => x.MobileNumber).NotEmpty();
        RuleFor(x => x.Email).NotEmpty();
    }
}

public class CreateLeadContactHandler : IRequestHandler<CreateLeadContactRequest, CreateLeadContactResult>
{
    private readonly ICommandRepository<LeadContact> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateLeadContactHandler(
        ICommandRepository<LeadContact> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateLeadContactResult> Handle(CreateLeadContactRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new LeadContact
        {
            CreatedById = request.CreatedById,
            LeadId = request.LeadId,
            Number = _numberSequenceService.GenerateNumber(nameof(LeadContact), "", "LC"),
            FullName = request.FullName,
            Description = request.Description,
            AddressStreet = request.AddressStreet,
            AddressCity = request.AddressCity,
            AddressState = request.AddressState,
            MobileNumber = request.MobileNumber,
            Email = request.Email
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateLeadContactResult
        {
            Data = entity
        };
    }
}