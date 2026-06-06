using System.Security.Cryptography;
using System.Text;

namespace OmmoBackend.Helpers
{
    public static class FieldCrypto
    {
        private static byte[] GetKey(IConfiguration config)
        {
            var base64Key = config["Crypto:AesKeyBase64"];
            if (string.IsNullOrWhiteSpace(base64Key))
                throw new Exception("Missing Crypto:AesKeyBase64");

            var key = Convert.FromBase64String(base64Key);
            if (key.Length != 32)
                throw new Exception("Key must be 32 bytes");

            return key;
        }

        public static string Encrypt(string plainText, IConfiguration config)
        {
            var key = GetKey(config);
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[16];

            using (var aes = new AesGcm(key))
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            byte[] packed = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, packed, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, packed, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, packed, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(packed);
        }

        public static string Decrypt(string encryptedBase64, IConfiguration config)
        {
            var key = GetKey(config);
            byte[] packed = Convert.FromBase64String(encryptedBase64);

            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            byte[] cipherBytes = new byte[packed.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(packed, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(packed, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(packed, nonce.Length + tag.Length, cipherBytes, 0, cipherBytes.Length);

            byte[] plainBytes = new byte[cipherBytes.Length];

            using (var aes = new AesGcm(key))
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }

}
