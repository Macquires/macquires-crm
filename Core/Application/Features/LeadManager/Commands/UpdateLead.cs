using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.LeadManager.Commands;

public class UpdateLeadResult
{
    public Lead? Data { get; set; }
}

public class UpdateLeadRequest : IRequest<UpdateLeadResult>
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? CompanyName { get; init; }
    public string? CompanyAddressStreet { get; init; }
    public string? CompanyAddressCity { get; init; }
    public string? CompanyAddressState { get; init; }
    public string? CompanyPhoneNumber { get; init; }
    public string? CompanyEmail { get; init; }
    public DateTime? DateProspecting { get; init; }
    public DateTime? DateClosingEstimation { get; init; }
    public DateTime? DateClosingActual { get; init; }
    public double? AmountTargeted { get; init; }
    public double? AmountClosed { get; init; }
    public double? BudgetScore { get; init; }
    public double? AuthorityScore { get; init; }
    public double? NeedScore { get; init; }
    public double? TimelineScore { get; init; }
    public string? PipelineStage { get; init; }
    public string? ClosingStatus { get; init; }
    public string? CampaignId { get; init; }
    public string? SalesTeamId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateLeadValidator : AbstractValidator<UpdateLeadRequest>
{
    public UpdateLeadValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.SalesTeamId).NotEmpty();
        RuleFor(x => x.CompanyName).NotEmpty();
        RuleFor(x => x.CompanyAddressStreet).NotEmpty();
        RuleFor(x => x.CompanyAddressCity).NotEmpty();
        RuleFor(x => x.CompanyAddressState).NotEmpty();
        RuleFor(x => x.CompanyPhoneNumber).NotEmpty();
        RuleFor(x => x.CompanyEmail).NotEmpty();
        RuleFor(x => x.DateProspecting).NotNull();
        RuleFor(x => x.DateClosingEstimation).NotNull();
        RuleFor(x => x.AmountTargeted).NotNull();
        RuleFor(x => x.AmountClosed).NotNull();
        RuleFor(x => x.BudgetScore).NotNull();
        RuleFor(x => x.AuthorityScore).NotNull();
        RuleFor(x => x.NeedScore).NotNull();
        RuleFor(x => x.TimelineScore).NotNull();
        RuleFor(x => x.PipelineStage).NotEmpty();
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}

public class UpdateLeadHandler : IRequestHandler<UpdateLeadRequest, UpdateLeadResult>
{
    private readonly ICommandRepository<Lead> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLeadHandler(
        ICommandRepository<Lead> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateLeadResult> Handle(UpdateLeadRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.Title = request.Title;
        entity.SalesTeamId = request.SalesTeamId;
        entity.Description = request.Description;
        entity.CompanyName = request.CompanyName;
        entity.CompanyAddressStreet = request.CompanyAddressStreet;
        entity.CompanyAddressCity = request.CompanyAddressCity;
        entity.CompanyAddressState = request.CompanyAddressState;
        entity.CompanyPhoneNumber = request.CompanyPhoneNumber;
        entity.CompanyEmail = request.CompanyEmail;
        entity.DateProspecting = request.DateProspecting;
        entity.DateClosingEstimation = request.DateClosingEstimation;
        entity.DateClosingActual = request.DateClosingActual;
        entity.AmountTargeted = request.AmountTargeted;
        entity.AmountClosed = request.AmountClosed;
        entity.BudgetScore = request.BudgetScore;
        entity.AuthorityScore = request.AuthorityScore;
        entity.NeedScore = request.NeedScore;
        entity.TimelineScore = request.TimelineScore;
        entity.PipelineStage = (PipelineStage)int.Parse(request.PipelineStage!);
        entity.CampaignId = request.CampaignId;

        if (!string.IsNullOrEmpty(request.ClosingStatus))
        {
            entity.ClosingStatus = (ClosingStatus)int.Parse(request.ClosingStatus!);
        }

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateLeadResult
        {
            Data = entity
        };
    }
}