using System.Security.Cryptography;
using System.Text;
using Application.Common.Security;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security.FieldEncryption;

public sealed class AesGcmFieldEncryptionService : IFieldEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;
    private readonly byte[] _searchHashKey;

    public AesGcmFieldEncryptionService(IOptions<FieldEncryptionOptions> options)
    {
        var o = options.Value;
        _key = Convert.FromBase64String(o.KeyBase64);
        _searchHashKey = Convert.FromBase64String(o.SearchHashKeyBase64);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("FieldEncryption:KeyBase64 must decode to 32 bytes.");
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        var payload = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, payload, NonceSize + TagSize, cipher.Length);
        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        var payload = Convert.FromBase64String(cipherText);
        if (payload.Length < NonceSize + TagSize + 1)
        {
            return cipherText;
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    public string ComputeSearchHash(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return "";
        var normalized = plainText.Trim();
        using var hmac = new HMACSHA256(_searchHashKey);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized)));
    }
}
