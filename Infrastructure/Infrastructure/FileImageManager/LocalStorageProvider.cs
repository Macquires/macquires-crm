using Application.Common.Services.FileImageManager;
using Microsoft.Extensions.Options;

namespace Infrastructure.FileImageManager;

public class LocalStorageProvider : IStorageProvider
{
    private const string PlaceholderFileName = "noimage.png";
    private readonly string _folderPath;

    public LocalStorageProvider(IOptions<FileImageSettings> settings)
    {
        _folderPath = Path.Combine(Directory.GetCurrentDirectory(), settings.Value.PathFolder);
        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }

        EnsurePlaceholderImage();
    }

    private void EnsurePlaceholderImage()
    {
        var placeholderPath = Path.Combine(_folderPath, PlaceholderFileName);
        if (File.Exists(placeholderPath))
        {
            return;
        }

        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "brand", "logo.png");
        if (File.Exists(logoPath))
        {
            File.Copy(logoPath, placeholderPath);
            return;
        }

        // 1x1 transparent PNG
        File.WriteAllBytes(
            placeholderPath,
            Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+X2ZkAAAAASUVORK5CYII="));
    }

    public async Task SaveAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_folderPath, path);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await stream.CopyToAsync(fileStream, cancellationToken);
    }

    public Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_folderPath, path);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true));
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_folderPath, path);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_folderPath, path);
        return Task.FromResult(File.Exists(filePath));
    }
}
