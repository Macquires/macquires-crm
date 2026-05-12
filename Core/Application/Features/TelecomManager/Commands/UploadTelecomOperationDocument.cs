using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class UploadTelecomOperationDocumentResult
{
    public TelecomOperationRequest? Data { get; set; }
}

public class UploadTelecomOperationDocumentRequest : IRequest<UploadTelecomOperationDocumentResult>
{
    public string Id { get; init; } = null!;
    public string? UpdatedById { get; init; }
}

public class UploadTelecomOperationDocumentValidator : AbstractValidator<UploadTelecomOperationDocumentRequest>
{
    public UploadTelecomOperationDocumentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

/// <summary>Marks ID document as uploaded (demo placeholder — no file storage in this command).</summary>
public class UploadTelecomOperationDocumentHandler : IRequestHandler<UploadTelecomOperationDocumentRequest, UploadTelecomOperationDocumentResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadTelecomOperationDocumentHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UploadTelecomOperationDocumentResult> Handle(UploadTelecomOperationDocumentRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        if (entity.Status != TelecomOperationStatus.Draft)
            throw new InvalidOperationException("Only draft operations accept document upload.");

        entity.DocumentStatus = TelecomDocumentStatus.Uploaded;
        entity.UpdatedById = request.UpdatedById;
        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UploadTelecomOperationDocumentResult { Data = entity };
    }
}
