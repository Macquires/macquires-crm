using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProgramManagerResourceManager.Commands;

public class CreateProgramManagerResourceResult
{
    public ProgramManagerResource? Data { get; set; }
}

public class CreateProgramManagerResourceRequest : IRequest<CreateProgramManagerResourceResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateProgramManagerResourceValidator : AbstractValidator<CreateProgramManagerResourceRequest>
{
    public CreateProgramManagerResourceValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateProgramManagerResourceHandler : IRequestHandler<CreateProgramManagerResourceRequest, CreateProgramManagerResourceResult>
{
    private readonly ICommandRepository<ProgramManagerResource> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProgramManagerResourceHandler(
        ICommandRepository<ProgramManagerResource> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateProgramManagerResourceResult> Handle(CreateProgramManagerResourceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new ProgramManagerResource();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateProgramManagerResourceResult
        {
            Data = entity
        };
    }
}