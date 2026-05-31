using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class UploadTelecomOperationIdentityDocumentResult
{
    public TelecomOperationRequest? Data { get; set; }
}

public class UploadTelecomOperationIdentityDocumentRequest : IRequest<UploadTelecomOperationIdentityDocumentResult>
{
    public string Id { get; init; } = null!;
    public Stream FileStream { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string? UpdatedById { get; init; }
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

    public UploadTelecomOperationIdentityDocumentHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        ITelecomOperationOrchestrator orchestrator,
        ITelecomOperationDocumentStore documentStore,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _orchestrator = orchestrator;
        _documentStore = documentStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<UploadTelecomOperationIdentityDocumentResult> Handle(
        UploadTelecomOperationIdentityDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        if (entity.Status != TelecomOperationStatus.Draft)
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

        await _orchestrator.TransitionAsync(
            entity,
            TelecomOperationStatus.PendingDocuments,
            request.UpdatedById,
            "رفع هوية — قيد التدقيق القانوني",
            cancellationToken);

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UploadTelecomOperationIdentityDocumentResult { Data = entity };
    }
}
