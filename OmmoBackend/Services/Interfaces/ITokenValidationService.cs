namespace OmmoBackend.Services.Interfaces
{
    public interface ITokenValidationService
    {
        /// <summary>
        /// Validates the JWT and returns claims on success, or error message on failure.
        /// </summary>
        Task<(bool IsValid, IDictionary<string, string>? Claims, string? ErrorMessage)> ValidateTokenAsync(string token);
    }
}
