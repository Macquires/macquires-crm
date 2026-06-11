namespace Application.Common.Services.FileImageManager;

public interface IStorageProvider
{
    Task SaveAsync(string path, Stream stream, CancellationToken cancellationToken = default);
    Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
}
