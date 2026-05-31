using Application.Common.Security;
using Infrastructure.DataAccessManager.EFCore.Converters;

namespace Application.Tests.Dashboard;

internal static class DashboardTestEncryption
{
    private static bool _initialized;

    internal static void EnsureInitialized()
    {
        if (_initialized) return;
        FieldEncryptionScope.Initialize(new PassthroughFieldEncryption());
        _initialized = true;
    }

    private sealed class PassthroughFieldEncryption : IFieldEncryptionService
    {
        public string Encrypt(string plain) => plain;
        public string Decrypt(string cipher) => cipher;
        public string ComputeSearchHash(string plain) => plain;
    }
}
