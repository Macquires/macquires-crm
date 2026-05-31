using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.VasManager.Commands;

public class DeleteValueAddedServiceResult
{
    public bool Success { get; init; }
}

public class DeleteValueAddedServiceRequest : IRequest<DeleteValueAddedServiceResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.TelecomVasManage;
    public string Id { get; init; } = "";
    public string? UpdatedById { get; init; }
}

public class DeleteValueAddedServiceValidator : AbstractValidator<DeleteValueAddedServiceRequest>
{
    public DeleteValueAddedServiceValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class DeleteValueAddedServiceHandler : IRequestHandler<DeleteValueAddedServiceRequest, DeleteValueAddedServiceResult>
{
    private readonly ICommandRepository<TelecomValueAddedService> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteValueAddedServiceHandler(ICommandRepository<TelecomValueAddedService> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteValueAddedServiceResult> Handle(
        DeleteValueAddedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("VAS service not found.");

        entity.IsDeleted = true;
        entity.UpdatedById = request.UpdatedById;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);
        return new DeleteValueAddedServiceResult { Success = true };
    }
}
