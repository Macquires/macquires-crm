using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BillManager.Commands;

public class DeleteBillResult
{
    public Bill? Data { get; set; }
}

public class DeleteBillRequest : IRequest<DeleteBillResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteBillValidator : AbstractValidator<DeleteBillRequest>
{
    public DeleteBillValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteBillHandler : IRequestHandler<DeleteBillRequest, DeleteBillResult>
{
    private readonly ICommandRepository<Bill> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBillHandler(
        ICommandRepository<Bill> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteBillResult> Handle(DeleteBillRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteBillResult
        {
            Data = entity
        };
    }
}