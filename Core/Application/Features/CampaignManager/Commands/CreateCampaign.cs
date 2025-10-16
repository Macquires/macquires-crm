using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.CampaignManager.Commands;

public class CreateCampaignResult
{
    public Campaign? Data { get; set; }
}

public class CreateCampaignRequest : IRequest<CreateCampaignResult>
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public double? TargetRevenueAmount { get; init; }
    public DateTime? CampaignDateStart { get; init; }
    public DateTime? CampaignDateFinish { get; init; }
    public string? SalesTeamId { get; init; }
    public string? Status { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateCampaignValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.SalesTeamId).NotEmpty();
        RuleFor(x => x.CampaignDateStart).NotEmpty();
        RuleFor(x => x.CampaignDateFinish).NotEmpty();
        RuleFor(x => x.TargetRevenueAmount).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public class CreateCampaignHandler : IRequestHandler<CreateCampaignRequest, CreateCampaignResult>
{
    private readonly ICommandRepository<Campaign> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateCampaignHandler(
        ICommandRepository<Campaign> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateCampaignResult> Handle(CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Campaign();
        entity.CreatedById = request.CreatedById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(Campaign), "", "CAM");
        entity.Title = request.Title;
        entity.SalesTeamId = request.SalesTeamId;
        entity.Description = request.Description;
        entity.TargetRevenueAmount = request.TargetRevenueAmount;
        entity.CampaignDateStart = request.CampaignDateStart;
        entity.CampaignDateFinish = request.CampaignDateFinish;
        entity.Status = (CampaignStatus)int.Parse(request.Status!);

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateCampaignResult
        {
            Data = entity
        };
    }
}