using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.BackOffice;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class UploadTelecomOperationIdentityDocumentResult
{
    public TelecomOperationRequest? Data { get; set; }
}

public class UploadTelecomOperationIdentityDocumentRequest : IRequest<UploadTelecomOperationIdentityDocumentResult>, IRequireAnyPermission
{
    public string Id { get; init; } = null!;
    public Stream FileStream { get; init; } = null!;
    public string FileName { get; init; } = null!;

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.CreateAny;
}

public class UploadTelecomOperationIdentityDocumentValidator : AbstractValidator<UploadTelecomOperationIdentityDocumentRequest>
{
    public UploadTelecomOperationIdentityDocumentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.FileStream).NotNull();
    }
}

/// <summary>Uploads identity scan and advances operation to <see cref="TelecomOperationStatus.PendingDocuments"/>.</summary>
public class UploadTelecomOperationIdentityDocumentHandler
    : IRequestHandler<UploadTelecomOperationIdentityDocumentRequest, UploadTelecomOperationIdentityDocumentResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly ITelecomOperationOrchestrator _orchestrator;
    private readonly ITelecomOperationDocumentStore _documentStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public UploadTelecomOperationIdentityDocumentHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        ITelecomOperationOrchestrator orchestrator,
        ITelecomOperationDocumentStore documentStore,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _orchestrator = orchestrator;
        _documentStore = documentStore;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<UploadTelecomOperationIdentityDocumentResult> Handle(
        UploadTelecomOperationIdentityDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        if (!BackOfficeTelecomPipelineState.CanAcceptDocumentUpload(entity))
        {
            throw new InvalidOperationException("Only draft operations accept identity document upload.");
        }

        var storageKey = await _documentStore.SaveIdentityDocumentAsync(
            entity.Id,
            request.FileStream,
            request.FileName,
            cancellationToken);

        entity.IdentityDocumentStorageKey = storageKey;
        entity.DocumentStatus = TelecomDocumentStatus.Uploaded;

        if (entity.Status == TelecomOperationStatus.Draft)
        {
            await _orchestrator.TransitionAsync(
                entity,
                TelecomOperationStatus.PendingDocuments,
                OperatorActor.RequireUserId(_operator),
                "رفع هوية — قيد التدقيق القانوني",
                cancellationToken);
        }

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UploadTelecomOperationIdentityDocumentResult { Data = entity };
    }
}
