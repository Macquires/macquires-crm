using Application.Common.Security;
using Application.Common.Services.FileDocumentManager;
using FluentValidation;
using MediatR;

namespace Application.Features.FileDocumentManager.Commands;

public class CreateDocumentResult
{
    public string? DocumentName { get; init; }
}

public class CreateDocumentRequest : IRequest<CreateDocumentResult>, IRequireAnyPermission
{
    public string? OriginalFileName { get; init; }
    public string? Extension { get; init; }
    public byte[]? Data { get; init; }
    public long? Size { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> PermissionKeys => FileAttachmentPermissionSets.UploadAny;
}

public class CreateDocumentValidator : AbstractValidator<CreateDocumentRequest>
{
    public CreateDocumentValidator()
    {
        RuleFor(x => x.OriginalFileName)
            .NotEmpty();

        RuleFor(x => x.Extension)
            .NotEmpty();

        RuleFor(x => x.Data)
            .NotEmpty();

        RuleFor(x => x.Size)
            .NotEmpty();
    }
}

public class CreateDocumentHandler : IRequestHandler<CreateDocumentRequest, CreateDocumentResult>
{
    private readonly IFileDocumentService _uploadDocument;
    private readonly IOperatorContext _operator;

    public CreateDocumentHandler(IFileDocumentService uploadDocument, IOperatorContext operatorContext)
    {
        _uploadDocument = uploadDocument;
        _operator = operatorContext;
    }

    public async Task<CreateDocumentResult> Handle(CreateDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await _uploadDocument.UploadAsync(
            request.OriginalFileName,
            request.Extension,
            request.Data,
            request.Size,
            request.Description,
            OperatorActor.RequireUserId(_operator),
            cancellationToken);

        return new CreateDocumentResult { DocumentName = result };
    }
}
