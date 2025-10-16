using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProgramManagerResourceManager.Commands;

public class DeleteProgramManagerResourceResult
{
    public ProgramManagerResource? Data { get; set; }
}

public class DeleteProgramManagerResourceRequest : IRequest<DeleteProgramManagerResourceResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteProgramManagerResourceValidator : AbstractValidator<DeleteProgramManagerResourceRequest>
{
    public DeleteProgramManagerResourceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteProgramManagerResourceHandler : IRequestHandler<DeleteProgramManagerResourceRequest, DeleteProgramManagerResourceResult>
{
    private readonly ICommandRepository<ProgramManagerResource> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProgramManagerResourceHandler(
        ICommandRepository<ProgramManagerResource> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteProgramManagerResourceResult> Handle(DeleteProgramManagerResourceRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteProgramManagerResourceResult
        {
            Data = entity
        };
    }
}

