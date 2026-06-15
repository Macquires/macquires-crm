using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.BackOffice;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class UploadTelecomOperationDocumentResult
{
    public TelecomOperationRequest? Data { get; set; }
}

public class UploadTelecomOperationDocumentRequest : IRequest<UploadTelecomOperationDocumentResult>, IRequireAnyPermission
{
    public string Id { get; init; } = null!;

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.CreateAny;
}

public class UploadTelecomOperationDocumentValidator : AbstractValidator<UploadTelecomOperationDocumentRequest>
{
    public UploadTelecomOperationDocumentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

/// <summary>Marks ID document as uploaded and advances pipeline to PendingDocuments.</summary>
public class UploadTelecomOperationDocumentHandler : IRequestHandler<UploadTelecomOperationDocumentRequest, UploadTelecomOperationDocumentResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly ITelecomOperationOrchestrator _orchestrator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public UploadTelecomOperationDocumentHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        ITelecomOperationOrchestrator orchestrator,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _orchestrator = orchestrator;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<UploadTelecomOperationDocumentResult> Handle(
        UploadTelecomOperationDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        if (!BackOfficeTelecomPipelineState.CanAcceptDocumentUpload(entity))
        {
            throw new InvalidOperationException("Only draft operations accept document upload.");
        }

        entity.DocumentStatus = TelecomDocumentStatus.Uploaded;
        if (entity.Status == TelecomOperationStatus.Draft)
        {
            await _orchestrator.TransitionAsync(
                entity,
                TelecomOperationStatus.PendingDocuments,
                OperatorActor.RequireUserId(_operator),
                "رفع الوثائق",
                cancellationToken);
        }

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UploadTelecomOperationDocumentResult { Data = entity };
    }
}
