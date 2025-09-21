namespace OmmoBackend.Helpers
{
    public interface ISecretProtector
    {
        string Protect(string plaintext);
        string Unprotect(string cipher);
    }
}
