using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BudgetManager.Commands;

public class DeleteBudgetResult
{
    public Budget? Data { get; set; }
}

public class DeleteBudgetRequest : IRequest<DeleteBudgetResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteBudgetValidator : AbstractValidator<DeleteBudgetRequest>
{
    public DeleteBudgetValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteBudgetHandler : IRequestHandler<DeleteBudgetRequest, DeleteBudgetResult>
{
    private readonly ICommandRepository<Budget> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBudgetHandler(
        ICommandRepository<Budget> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteBudgetResult> Handle(DeleteBudgetRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteBudgetResult
        {
            Data = entity
        };
    }
}