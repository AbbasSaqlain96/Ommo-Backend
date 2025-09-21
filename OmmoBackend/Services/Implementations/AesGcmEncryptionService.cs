using OmmoBackend.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace OmmoBackend.Services.Implementations
{
    public class AesGcmEncryptionService : IEncryptionService
    {
        private readonly byte[] _key; // 32 bytes for AES-256

        public AesGcmEncryptionService(IConfiguration config)
        {
            var keyBase64 = config["Encryption:Key"];
            if (string.IsNullOrWhiteSpace(keyBase64))
                throw new InvalidOperationException("Encryption key missing in configuration.");

            _key = Convert.FromBase64String(keyBase64);
            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption key must be 32 bytes (Base64).");
        }

        public string Encrypt(string plain)
        {
            if (plain == null) return null;

            using var aes = new AesGcm(_key);

            var nonce = RandomNumberGenerator.GetBytes(12); // 96-bit nonce
            var plainBytes = Encoding.UTF8.GetBytes(plain);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[16];

            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Concatenate nonce + tag + ciphertext
            var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string cipherText)
        {
            if (cipherText == null) return null;

            var fullCipher = Convert.FromBase64String(cipherText);

            var nonce = new byte[12];
            var tag = new byte[16];
            var cipherBytes = new byte[fullCipher.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(fullCipher, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(fullCipher, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(fullCipher, nonce.Length + tag.Length, cipherBytes, 0, cipherBytes.Length);

            var plainBytes = new byte[cipherBytes.Length];
            using var aes = new AesGcm(_key);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
