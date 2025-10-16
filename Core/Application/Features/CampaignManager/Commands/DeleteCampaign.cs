using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CampaignManager.Commands;

public class DeleteCampaignResult
{
    public Campaign? Data { get; set; }
}

public class DeleteCampaignRequest : IRequest<DeleteCampaignResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteCampaignValidator : AbstractValidator<DeleteCampaignRequest>
{
    public DeleteCampaignValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteCampaignHandler : IRequestHandler<DeleteCampaignRequest, DeleteCampaignResult>
{
    private readonly ICommandRepository<Campaign> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCampaignHandler(
        ICommandRepository<Campaign> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteCampaignResult> Handle(DeleteCampaignRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteCampaignResult
        {
            Data = entity
        };
    }
}