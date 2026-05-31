using Application.Common.Security;

namespace Infrastructure.DataAccessManager.EFCore.Converters;

/// <summary>Static bridge for EF value converters (initialized at startup).</summary>
public static class FieldEncryptionScope
{
    private static IFieldEncryptionService? _service;

    public static void Initialize(IFieldEncryptionService service) =>
        _service = service ?? throw new ArgumentNullException(nameof(service));

    public static string Encrypt(string plain) =>
        _service?.Encrypt(plain) ?? throw new InvalidOperationException("Field encryption is not initialized.");

    public static string Decrypt(string cipher) =>
        _service?.Decrypt(cipher) ?? throw new InvalidOperationException("Field encryption is not initialized.");
}
