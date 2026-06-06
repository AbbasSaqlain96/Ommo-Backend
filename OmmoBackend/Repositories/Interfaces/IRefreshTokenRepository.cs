using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task SaveRefreshTokenAsync(int userId, string refreshToken);
        Task<RefreshToken> ConsumeRefreshTokenAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenSafeAsync(string refreshToken);
        Task<bool> TryRevokeRefreshTokenAsync(string refreshToken);
    }
}