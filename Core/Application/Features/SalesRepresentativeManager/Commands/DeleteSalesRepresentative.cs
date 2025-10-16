using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesRepresentativeManager.Commands;

public class DeleteSalesRepresentativeResult
{
    public SalesRepresentative? Data { get; set; }
}

public class DeleteSalesRepresentativeRequest : IRequest<DeleteSalesRepresentativeResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteSalesRepresentativeValidator : AbstractValidator<DeleteSalesRepresentativeRequest>
{
    public DeleteSalesRepresentativeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteSalesRepresentativeHandler : IRequestHandler<DeleteSalesRepresentativeRequest, DeleteSalesRepresentativeResult>
{
    private readonly ICommandRepository<SalesRepresentative> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSalesRepresentativeHandler(
        ICommandRepository<SalesRepresentative> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteSalesRepresentativeResult> Handle(DeleteSalesRepresentativeRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteSalesRepresentativeResult
        {
            Data = entity
        };
    }
}