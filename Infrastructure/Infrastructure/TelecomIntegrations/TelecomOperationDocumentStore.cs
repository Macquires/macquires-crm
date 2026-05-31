using Application.Common.Telecom;

namespace Infrastructure.TelecomIntegrations;

public sealed class TelecomOperationDocumentStore : ITelecomOperationDocumentStore
{
    private const int MaxBytes = 8 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly string _rootFolder;

    public TelecomOperationDocumentStore()
    {
        _rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "telecom-operation-documents");
        Directory.CreateDirectory(_rootFolder);
    }

    public async Task<string> SaveIdentityDocumentAsync(
        string operationId,
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("يُسمح فقط بملفات PDF أو صور (JPG/PNG/WebP).");
        }

        if (content.CanSeek && content.Length > MaxBytes)
        {
            throw new InvalidOperationException("حجم ملف الهوية يتجاوز 8 ميجابايت.");
        }

        var safeOp = SanitizeSegment(operationId);
        var relative = $"{safeOp}/identity{ext}";
        var absolute = GetAbsolutePath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);

        await using var file = File.Create(absolute);
        await content.CopyToAsync(file, cancellationToken);
        return relative.Replace('\\', '/');
    }

    public string GetAbsolutePath(string storageKey)
    {
        var combined = Path.Combine(_rootFolder, storageKey.Replace('/', Path.DirectorySeparatorChar));
        var full = Path.GetFullPath(combined);
        var rootFull = Path.GetFullPath(_rootFolder);
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("مسار الوثيقة غير صالح.");
        }

        return full;
    }

    public bool Exists(string? storageKey) =>
        !string.IsNullOrWhiteSpace(storageKey) && File.Exists(GetAbsolutePath(storageKey));

    private static string SanitizeSegment(string value)
    {
        var chars = value.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray();
        return chars.Length > 0 ? new string(chars) : "op";
    }
}
