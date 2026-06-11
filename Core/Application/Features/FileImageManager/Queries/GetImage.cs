using Application.Common.Services.FileImageManager;
using FluentValidation;
using MediatR;

namespace Application.Features.FileImageManager.Queries;


public class GetImageResult
{
    public byte[]? Data { get; init; }
}

public class GetImageRequest : IRequest<GetImageResult>
{
    public string? ImageName { get; init; }
}

public class GetImageValidator : AbstractValidator<GetImageRequest>
{
    public GetImageValidator()
    {
        RuleFor(x => x.ImageName)
            .NotEmpty();
    }
}

public class GetImageHandler : IRequestHandler<GetImageRequest, GetImageResult>
{
    private readonly IFileImageService _imageService;

    public GetImageHandler(IFileImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task<GetImageResult> Handle(GetImageRequest request, CancellationToken cancellationToken)
    {
        await using var stream = await _imageService.GetFileStreamAsync(request.ImageName ?? "", cancellationToken);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return new GetImageResult { Data = memory.ToArray() };
    }
}

