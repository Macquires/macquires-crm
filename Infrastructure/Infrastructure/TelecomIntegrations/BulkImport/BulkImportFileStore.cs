using Application.Common.BulkImport;
using Microsoft.Extensions.Options;

namespace Infrastructure.TelecomIntegrations.BulkImport;

public sealed class BulkImportFileStore : IBulkImportFileStore
{
    private readonly string _rootFolder;
    private readonly int _maxBytes;

    public BulkImportFileStore(IOptions<BulkImportOptions> options)
    {
        var settings = options.Value;
        _rootFolder = Path.Combine(Directory.GetCurrentDirectory(), settings.PathFolder);
        _maxBytes = settings.MaxFileSizeMb * 1024 * 1024;
        Directory.CreateDirectory(_rootFolder);
    }

    public async Task<string> SaveUploadAsync(string jobId, Stream content, string originalFileName, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (ext is not ".csv" and not ".txt")
        {
            throw new InvalidOperationException("يُسمح فقط بملفات CSV أو TXT.");
        }

        if (content.CanSeek && content.Length > _maxBytes)
        {
            throw new InvalidOperationException($"حجم الملف يتجاوز الحد {_maxBytes / (1024 * 1024)} ميجابايت.");
        }

        var relative = $"{jobId}{ext}";
        var absolute = GetAbsolutePath(relative);
        await using var file = File.Create(absolute);
        await content.CopyToAsync(file, cancellationToken);
        return relative;
    }

    public string GetAbsolutePath(string storedRelativePath)
    {
        var combined = Path.Combine(_rootFolder, storedRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var full = Path.GetFullPath(combined);
        var rootFull = Path.GetFullPath(_rootFolder);
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("مسار الملف غير صالح.");
        }

        return full;
    }

    public async Task<int> CountDataRowsAsync(string storedRelativePath, CancellationToken cancellationToken)
    {
        var path = GetAbsolutePath(storedRelativePath);
        await using var stream = File.OpenRead(path);
        return await BulkImportCsvParser.CountDataRowsAsync(stream, cancellationToken);
    }

    public IAsyncEnumerable<ParsedImportRow> ReadRowsAsync(string storedRelativePath, CancellationToken cancellationToken)
    {
        var path = GetAbsolutePath(storedRelativePath);
        return BulkImportCsvParser.ReadRowsAsync(path, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ReadHeaderColumnsAsync(string storedRelativePath, CancellationToken cancellationToken)
    {
        var path = GetAbsolutePath(storedRelativePath);
        return await BulkImportCsvParser.ReadHeaderColumnsAsync(path, cancellationToken);
    }
}
