using Microsoft.AspNetCore.DataProtection;

namespace OmmoBackend.Helpers
{
    public class DataProtectionSecretProtector : ISecretProtector
    {
        private readonly IDataProtector _protector;
        public DataProtectionSecretProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("IntegrationCredentialsV1");
        }

        public string Protect(string plaintext) => _protector.Protect(plaintext);
        public string Unprotect(string cipher) => _protector.Unprotect(cipher);
    }
}
