using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.ExpenseManager.Commands;

public class CreateExpenseResult
{
    public Expense? Data { get; set; }
}

public class CreateExpenseRequest : IRequest<CreateExpenseResult>
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? ExpenseDate { get; init; }
    public string? Status { get; init; }
    public double? Amount { get; init; }
    public string? CampaignId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateExpenseValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.ExpenseDate).NotNull();
        RuleFor(x => x.Amount).NotNull();
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public class CreateExpenseHandler : IRequestHandler<CreateExpenseRequest, CreateExpenseResult>
{
    private readonly ICommandRepository<Expense> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateExpenseHandler(
        ICommandRepository<Expense> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateExpenseResult> Handle(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Expense
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(Expense), "", "EXP"),
            Title = request.Title,
            Description = request.Description,
            ExpenseDate = request.ExpenseDate,
            Status = (ExpenseStatus)int.Parse(request.Status!),
            Amount = request.Amount,
            CampaignId = request.CampaignId
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateExpenseResult
        {
            Data = entity
        };
    }
}