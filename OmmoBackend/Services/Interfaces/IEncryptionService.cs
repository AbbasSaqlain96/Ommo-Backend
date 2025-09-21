namespace OmmoBackend.Services.Interfaces
{
    public interface IEncryptionService
    {
        string Encrypt(string plain);
        string Decrypt(string cipherText);
    }
}
