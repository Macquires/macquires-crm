namespace Application.Common.Security;

public interface IFieldEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string ComputeSearchHash(string plainText);
}
