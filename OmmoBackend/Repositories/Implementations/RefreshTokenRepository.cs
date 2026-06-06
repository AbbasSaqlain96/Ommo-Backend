using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Exceptions;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<RefreshTokenRepository> _logger;
        public RefreshTokenRepository(AppDbContext dbContext, ILogger<RefreshTokenRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SaveRefreshTokenAsync(int userId, string refreshToken)
        {
            var refreshTokenEntity = CreateRefreshTokenEntity(userId, refreshToken);

            try
            {
                _logger.LogInformation("Saving refresh token for user ID: {UserId}", userId);

                _dbContext.refresh_tokens.Add(refreshTokenEntity);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully saved refresh token for user ID: {UserId}", userId);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error occurred while saving the refresh token for user ID: {UserId}", userId);
                throw new DataAccessException("An error occurred while saving the refresh token.", dbEx);
            }
        }

        public async Task<RefreshToken?> ConsumeRefreshTokenAsync(string refreshToken)
        {
            var token = await _dbContext.refresh_tokens
                .FirstOrDefaultAsync(rt => rt.refresh_token == refreshToken);

            if (token == null)
                return null;

            if (token.is_revoked || token.is_used)
                return null;

            if (token.expiration_time < DateTime.UtcNow)
                return null;

            // Mark as used + revoked (single logical step)
            token.is_used = true;
            token.used_at = DateTime.UtcNow;
            token.is_revoked = true;
            token.revoked_at = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return token;
        }

        public async Task<bool> RevokeRefreshTokenSafeAsync(string refreshToken)
        {
            var token = await _dbContext.refresh_tokens
                .FirstOrDefaultAsync(rt => rt.refresh_token == refreshToken);

            if (token == null)
                return false;

            if (token.is_revoked)
                return true; // already revoked - OK

            token.is_revoked = true;
            token.revoked_at = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TryRevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _dbContext.refresh_tokens
                .FirstOrDefaultAsync(rt => rt.refresh_token == refreshToken);

            if (token == null)
                return false;

            if (!token.is_revoked)
            {
                token.is_revoked = true;
                token.revoked_at = DateTime.UtcNow;
            }

            if (!token.is_used)
            {
                token.is_used = true;
                token.used_at = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        private RefreshToken CreateRefreshTokenEntity(int userId, string refreshToken)
        {
            return new RefreshToken
            {
                refresh_token = refreshToken,
                user_id = userId,
                expiration_time = DateTime.Now.AddDays(7), // 7-day refresh token
                created_at = DateTime.Now
            };
        }
    }
}