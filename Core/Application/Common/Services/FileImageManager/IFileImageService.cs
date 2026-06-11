namespace Application.Common.Services.FileImageManager;
public interface IFileImageService
{
    Task<string> UploadAsync(
        string? originalFileName,
        string? docExtension,
        Stream fileStream,
        long? size,
        string? description = "",
        string? createdById = "",
        CancellationToken cancellationToken = default);
    Task<Stream> GetFileStreamAsync(string fileName, CancellationToken cancellationToken = default);
}
