using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.LeadContactManager.Commands;

public class DeleteLeadContactResult
{
    public LeadContact? Data { get; set; }
}

public class DeleteLeadContactRequest : IRequest<DeleteLeadContactResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteLeadContactValidator : AbstractValidator<DeleteLeadContactRequest>
{
    public DeleteLeadContactValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteLeadContactHandler : IRequestHandler<DeleteLeadContactRequest, DeleteLeadContactResult>
{
    private readonly ICommandRepository<LeadContact> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLeadContactHandler(
        ICommandRepository<LeadContact> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteLeadContactResult> Handle(DeleteLeadContactRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteLeadContactResult
        {
            Data = entity
        };
    }
}