using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.LeadManager.Commands;

public class DeleteLeadResult
{
    public Lead? Data { get; set; }
}

public class DeleteLeadRequest : IRequest<DeleteLeadResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteLeadValidator : AbstractValidator<DeleteLeadRequest>
{
    public DeleteLeadValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteLeadHandler : IRequestHandler<DeleteLeadRequest, DeleteLeadResult>
{
    private readonly ICommandRepository<Lead> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLeadHandler(
        ICommandRepository<Lead> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteLeadResult> Handle(DeleteLeadRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteLeadResult
        {
            Data = entity
        };
    }
}