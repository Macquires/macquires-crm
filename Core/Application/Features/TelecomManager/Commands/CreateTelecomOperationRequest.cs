using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public static class TelecomNumberSequence
{
    public static (string EntityName, string Prefix) ForKind(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.Migration => ("TelecomOp_Migration", "MGR-"),
        TelecomOperationKind.TakeOver => ("TelecomOp_TakeOver", "TKO-"),
        TelecomOperationKind.NewActivation => ("TelecomOp_Activation", "ACT-"),
        TelecomOperationKind.SimSwap => ("TelecomOp_SimSwap", "SIM-"),
        TelecomOperationKind.ServiceModification => ("TelecomOp_Vas", "VAS-"),
        TelecomOperationKind.NumberPortability => ("TelecomOp_Mnp", "MNP-"),
        _ => ("TelecomOp_Generic", "TEL-")
    };
}

public class CreateTelecomOperationRequestResult
{
    public TelecomOperationRequest? Data { get; set; }
}

public class CreateTelecomOperationRequest : IRequest<CreateTelecomOperationRequestResult>
{
    public TelecomOperationKind Kind { get; init; }
    public string SubscriberProfileId { get; init; } = null!;
    public string? SecondarySubscriberProfileId { get; init; }
    public string? MsisdnAssetId { get; init; }
    public string? ProductId { get; init; }
    public string? Notes { get; init; }
    public string? TargetOfferName { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateTelecomOperationRequestValidator : AbstractValidator<CreateTelecomOperationRequest>
{
    public CreateTelecomOperationRequestValidator()
    {
        RuleFor(x => x.SubscriberProfileId).NotEmpty();
        RuleFor(x => x.SecondarySubscriberProfileId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TakeOver);
    }
}

public class CreateTelecomOperationRequestHandler : IRequestHandler<CreateTelecomOperationRequest, CreateTelecomOperationRequestResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateTelecomOperationRequestHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateTelecomOperationRequestResult> Handle(CreateTelecomOperationRequest request, CancellationToken cancellationToken)
    {
        var (entityName, prefix) = TelecomNumberSequence.ForKind(request.Kind);
        var number = _numberSequenceService.GenerateNumber(entityName, prefix, "", useDate: false);

        var entity = new TelecomOperationRequest
        {
            Kind = request.Kind,
            Number = number,
            Status = TelecomOperationStatus.Draft,
            DocumentStatus = TelecomDocumentStatus.Missing,
            SubscriberProfileId = request.SubscriberProfileId,
            SecondarySubscriberProfileId = request.SecondarySubscriberProfileId,
            MsisdnAssetId = request.MsisdnAssetId,
            ProductId = request.ProductId,
            Notes = request.Notes,
            TargetOfferName = request.TargetOfferName,
            CreatedById = request.CreatedById
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateTelecomOperationRequestResult { Data = entity };
    }
}
