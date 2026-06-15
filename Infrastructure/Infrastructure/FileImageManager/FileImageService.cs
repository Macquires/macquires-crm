using Application.Common.Repositories;
using Application.Common.Services.FileImageManager;
using Domain.Entities;
using Microsoft.Extensions.Options;

namespace Infrastructure.FileImageManager;

public class FileImageService : IFileImageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageProvider _storageProvider;
    private readonly int _maxFileSizeInBytes;
    private readonly ICommandRepository<FileImage> _docRepository;

    public FileImageService(
        IUnitOfWork unitOfWork,
        IOptions<FileImageSettings> settings,
        ICommandRepository<FileImage> docRepository,
        IStorageProvider storageProvider
        )
    {
        _unitOfWork = unitOfWork;
        _storageProvider = storageProvider;
        _maxFileSizeInBytes = settings.Value.MaxFileSizeInMB * 1024 * 1024;
        _docRepository = docRepository;
    }

    public async Task<string> UploadAsync(
        string? originalFileName,
        string? docExtension,
        Stream fileStream,
        long? size,
        string? description = "",
        string? createdById = "",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(docExtension) || docExtension.Contains(Path.DirectorySeparatorChar) || docExtension.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new Exception($"Invalid file extension: {nameof(docExtension)}");
        }

        if (fileStream == null || fileStream.Length == 0)
        {
            throw new Exception("File stream cannot be null or empty.");
        }

        if (size > _maxFileSizeInBytes)
        {
            throw new Exception($"File size exceeds the maximum allowed size of {_maxFileSizeInBytes / (1024 * 1024)} MB");
        }

        var fileName = $"{Guid.NewGuid():N}.{docExtension}";

        // Zero-allocation streaming to storage provider
        await _storageProvider.SaveAsync(fileName, fileStream, cancellationToken);

        var img = new FileImage
        {
            Name = fileName,
            OriginalName = originalFileName,
            Extension = docExtension,
            GeneratedName = fileName,
            FileSize = size,
            Description = description,
            CreatedById = createdById
        };

        await _docRepository.CreateAsync(img, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return fileName;
    }

    public async Task<Stream> GetFileStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || string.Equals(fileName, "undefined", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "null", StringComparison.OrdinalIgnoreCase))
        {
            fileName = "noimage.png";
        }

        if (await _storageProvider.ExistsAsync(fileName, cancellationToken))
        {
            return await _storageProvider.GetAsync(fileName, cancellationToken);
        }

        if (!string.Equals(fileName, "noimage.png", StringComparison.OrdinalIgnoreCase)
            && await _storageProvider.ExistsAsync("noimage.png", cancellationToken))
        {
            return await _storageProvider.GetAsync("noimage.png", cancellationToken);
        }

        throw new FileNotFoundException("The requested file was not found in storage.", fileName);
    }
}
