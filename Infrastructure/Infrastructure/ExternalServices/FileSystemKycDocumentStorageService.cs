using Application.Common.Telecom;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ExternalServices;

public sealed class FileSystemKycDocumentStorageService : IKycDocumentStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg"
    };

    private readonly string _vaultRoot;
    private readonly int _maxBytes;
    private readonly ILogger<FileSystemKycDocumentStorageService> _logger;

    public FileSystemKycDocumentStorageService(
        IHostEnvironment hostEnvironment,
        IOptions<KycDocumentStorageOptions> options,
        ILogger<FileSystemKycDocumentStorageService> logger)
    {
        _logger = logger;
        var configured = (options.Value.VaultRootPath ?? string.Empty).Trim();
        _vaultRoot = Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configured));
        _maxBytes = options.Value.MaxFileSizeBytes > 0
            ? options.Value.MaxFileSizeBytes
            : 5 * 1024 * 1024;

        Directory.CreateDirectory(_vaultRoot);
        _logger.LogInformation("KYC vault initialized at {VaultRoot}", _vaultRoot);
    }

    public async Task<string> StoreKycDocumentAsync(
        string msisdn,
        string fileName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("يُسمح فقط بملفات PDF أو صور (JPG/PNG).");
        }

        if (fileStream.CanSeek && fileStream.Length > _maxBytes)
        {
            throw new InvalidOperationException("حجم ملف KYC يتجاوز 5 ميجابايت.");
        }

        var documentReferenceId = Guid.CreateVersion7().ToString();
        var safeMsisdn = SanitizeSegment(msisdn);
        var relative = $"{safeMsisdn}/{documentReferenceId}{ext}";
        var absolute = GetAbsolutePath(relative);

        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);

        await using (var target = File.Create(absolute))
        {
            await fileStream.CopyToAsync(target, cancellationToken);
        }

        if (new FileInfo(absolute).Length > _maxBytes)
        {
            File.Delete(absolute);
            throw new InvalidOperationException("حجم ملف KYC يتجاوز 5 ميجابايت.");
        }

        _logger.LogInformation(
            "Stored KYC document {DocumentReferenceId} for MSISDN {Msisdn} ({ContentType}, {Bytes} bytes).",
            documentReferenceId,
            safeMsisdn,
            contentType,
            new FileInfo(absolute).Length);

        return documentReferenceId;
    }

    public Task<Stream> GetKycDocumentAsync(
        string documentReferenceId,
        CancellationToken cancellationToken = default)
    {
        var path = ResolveDocumentPath(documentReferenceId)
            ?? throw new FileNotFoundException("وثيقة KYC غير موجودة.", documentReferenceId);

        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public bool DocumentExists(string? documentReferenceId)
    {
        if (string.IsNullOrWhiteSpace(documentReferenceId))
        {
            return false;
        }

        return ResolveDocumentPath(documentReferenceId.Trim()) != null;
    }

    public Task<(Stream Stream, string ContentType)?> TryOpenKycDocumentAsync(
        string documentReferenceId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var path = ResolveDocumentPath(documentReferenceId.Trim());
        if (path == null)
        {
            return Task.FromResult<(Stream Stream, string ContentType)?>(null);
        }

        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        Stream stream = File.OpenRead(path);
        return Task.FromResult<(Stream Stream, string ContentType)?>((stream, contentType));
    }

    private string? ResolveDocumentPath(string documentReferenceId)
    {
        var safeId = SanitizeSegment(documentReferenceId);
        if (!string.Equals(safeId, documentReferenceId, StringComparison.Ordinal))
        {
            return null;
        }

        foreach (var ext in AllowedExtensions)
        {
            var matches = Directory.EnumerateFiles(_vaultRoot, $"{safeId}{ext}", SearchOption.AllDirectories);
            var match = matches.FirstOrDefault();
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private string GetAbsolutePath(string relativeKey)
    {
        var combined = Path.Combine(_vaultRoot, relativeKey.Replace('/', Path.DirectorySeparatorChar));
        var full = Path.GetFullPath(combined);
        var rootFull = Path.GetFullPath(_vaultRoot);
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("مسار وثيقة KYC غير صالح.");
        }

        return full;
    }

    private static string SanitizeSegment(string value)
    {
        var chars = value.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray();
        return chars.Length > 0 ? new string(chars) : "unknown";
    }
}
