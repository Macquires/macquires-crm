using System.IO;
using Application.Common.Security;
using Application.Common.Services.FileImageManager;
using FluentValidation;
using MediatR;

namespace Application.Features.FileImageManager.Commands;

public class CreateImageResult
{
    public string? ImageName { get; init; }
}

public class CreateImageRequest : IRequest<CreateImageResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => FileAttachmentPermissionSets.UploadAny;
    public string? OriginalFileName { get; init; }
    public string? Extension { get; init; }
    public byte[]? Data { get; init; }
    public long? Size { get; init; }
    public string? Description { get; init; }
}

public class CreateImageValidator : AbstractValidator<CreateImageRequest>
{
    public CreateImageValidator()
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

public class CreateImageHandler : IRequestHandler<CreateImageRequest, CreateImageResult>
{
    private readonly IFileImageService _uploadImage;
    private readonly IOperatorContext _operator;

    public CreateImageHandler(IFileImageService uploadImage, IOperatorContext operatorContext)
    {
        _uploadImage = uploadImage;
        _operator = operatorContext;
    }

    public async Task<CreateImageResult> Handle(CreateImageRequest request, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(request.Data!);
        var result = await _uploadImage.UploadAsync(
            request.OriginalFileName,
            request.Extension,
            stream,
            request.Size,
            request.Description,
            OperatorActor.RequireUserId(_operator),
            cancellationToken);

        return new CreateImageResult { ImageName = result };
    }
}
