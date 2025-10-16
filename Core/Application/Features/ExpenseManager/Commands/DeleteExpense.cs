using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ExpenseManager.Commands;

public class DeleteExpenseResult
{
    public Expense? Data { get; set; }
}

public class DeleteExpenseRequest : IRequest<DeleteExpenseResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteExpenseValidator : AbstractValidator<DeleteExpenseRequest>
{
    public DeleteExpenseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteExpenseHandler : IRequestHandler<DeleteExpenseRequest, DeleteExpenseResult>
{
    private readonly ICommandRepository<Expense> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteExpenseHandler(
        ICommandRepository<Expense> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteExpenseResult> Handle(DeleteExpenseRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteExpenseResult
        {
            Data = entity
        };
    }
}