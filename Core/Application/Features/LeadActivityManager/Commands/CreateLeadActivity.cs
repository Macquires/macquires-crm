using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.LeadActivityManager.Commands;

public class CreateLeadActivityResult
{
    public LeadActivity? Data { get; set; }
}

public class CreateLeadActivityRequest : IRequest<CreateLeadActivityResult>
{
    public string? LeadId { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Type { get; init; }
    public string? AttachmentName { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateLeadActivityValidator : AbstractValidator<CreateLeadActivityRequest>
{
    public CreateLeadActivityValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.Summary).NotEmpty();
        RuleFor(x => x.FromDate).NotNull();
        RuleFor(x => x.ToDate).NotNull();
        RuleFor(x => x.Type).NotEmpty();
    }
}

public class CreateLeadActivityHandler : IRequestHandler<CreateLeadActivityRequest, CreateLeadActivityResult>
{
    private readonly ICommandRepository<LeadActivity> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateLeadActivityHandler(
        ICommandRepository<LeadActivity> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateLeadActivityResult> Handle(CreateLeadActivityRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new LeadActivity
        {
            CreatedById = request.CreatedById,
            LeadId = request.LeadId,
            Number = _numberSequenceService.GenerateNumber(nameof(LeadActivity), "", "LA"),
            Summary = request.Summary,
            Description = request.Description,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Type = (LeadActivityType)int.Parse(request.Type!),
            AttachmentName = request.AttachmentName
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateLeadActivityResult
        {
            Data = entity
        };
    }
}