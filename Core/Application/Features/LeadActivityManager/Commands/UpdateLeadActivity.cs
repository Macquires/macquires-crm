using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.LeadActivityManager.Commands;

public class UpdateLeadActivityResult
{
    public LeadActivity? Data { get; set; }
}

public class UpdateLeadActivityRequest : IRequest<UpdateLeadActivityResult>
{
    public string? Id { get; init; }
    public string? LeadId { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Type { get; init; }
    public string? AttachmentName { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateLeadActivityValidator : AbstractValidator<UpdateLeadActivityRequest>
{
    public UpdateLeadActivityValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Summary).NotEmpty();
        RuleFor(x => x.FromDate).NotNull();
        RuleFor(x => x.ToDate).NotNull();
        RuleFor(x => x.Type).NotEmpty();
    }
}

public class UpdateLeadActivityHandler : IRequestHandler<UpdateLeadActivityRequest, UpdateLeadActivityResult>
{
    private readonly ICommandRepository<LeadActivity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLeadActivityHandler(
        ICommandRepository<LeadActivity> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateLeadActivityResult> Handle(UpdateLeadActivityRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.LeadId = request.LeadId;
        entity.Summary = request.Summary;
        entity.Description = request.Description;
        entity.FromDate = request.FromDate;
        entity.ToDate = request.ToDate;
        entity.Type = (LeadActivityType)int.Parse(request.Type!);
        entity.AttachmentName = request.AttachmentName;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateLeadActivityResult
        {
            Data = entity
        };
    }
}