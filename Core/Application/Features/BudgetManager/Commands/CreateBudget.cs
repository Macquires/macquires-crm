using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.BudgetManager.Commands;

public class CreateBudgetResult
{
    public Budget? Data { get; set; }
}

public class CreateBudgetRequest : IRequest<CreateBudgetResult>
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? BudgetDate { get; init; }
    public string? Status { get; init; }
    public double? Amount { get; init; }
    public string? CampaignId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateBudgetValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.BudgetDate).NotNull();
        RuleFor(x => x.Amount).NotNull();
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public class CreateBudgetHandler : IRequestHandler<CreateBudgetRequest, CreateBudgetResult>
{
    private readonly ICommandRepository<Budget> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateBudgetHandler(
        ICommandRepository<Budget> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateBudgetResult> Handle(CreateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Budget
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(Budget), "", "BUD"),
            Title = request.Title,
            Description = request.Description,
            BudgetDate = request.BudgetDate,
            Status = (BudgetStatus)int.Parse(request.Status!),
            Amount = request.Amount,
            CampaignId = request.CampaignId
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateBudgetResult
        {
            Data = entity
        };
    }
}