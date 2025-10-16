using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.LeadActivityManager.Commands;

public class DeleteLeadActivityResult
{
    public LeadActivity? Data { get; set; }
}

public class DeleteLeadActivityRequest : IRequest<DeleteLeadActivityResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteLeadActivityValidator : AbstractValidator<DeleteLeadActivityRequest>
{
    public DeleteLeadActivityValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteLeadActivityHandler : IRequestHandler<DeleteLeadActivityRequest, DeleteLeadActivityResult>
{
    private readonly ICommandRepository<LeadActivity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLeadActivityHandler(
        ICommandRepository<LeadActivity> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteLeadActivityResult> Handle(DeleteLeadActivityRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteLeadActivityResult
        {
            Data = entity
        };
    }
}