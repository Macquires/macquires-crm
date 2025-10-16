using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProgramManagerResourceManager.Commands;

public class UpdateProgramManagerResourceResult
{
    public ProgramManagerResource? Data { get; set; }
}

public class UpdateProgramManagerResourceRequest : IRequest<UpdateProgramManagerResourceResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateProgramManagerResourceValidator : AbstractValidator<UpdateProgramManagerResourceRequest>
{
    public UpdateProgramManagerResourceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateProgramManagerResourceHandler : IRequestHandler<UpdateProgramManagerResourceRequest, UpdateProgramManagerResourceResult>
{
    private readonly ICommandRepository<ProgramManagerResource> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProgramManagerResourceHandler(
        ICommandRepository<ProgramManagerResource> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateProgramManagerResourceResult> Handle(UpdateProgramManagerResourceRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateProgramManagerResourceResult
        {
            Data = entity
        };
    }
}

