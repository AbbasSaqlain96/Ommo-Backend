using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Constants;
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

        public async Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Fetching refresh token: {RefreshToken}", refreshToken);

                var token = await _dbContext.refresh_tokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rt => rt.refresh_token == refreshToken && !rt.is_revoked);

                if (token == null)
                {
                    _logger.LogWarning("Refresh token not found or has been revoked: {RefreshToken}", refreshToken);
                }
                else
                {
                    _logger.LogInformation("Successfully retrieved refresh token: {RefreshToken}", refreshToken);
                }

                return token;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving refresh token: {RefreshToken}", refreshToken);
                throw new KeyNotFoundException("Refresh token not found or has been revoked.", ex);
            }
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Revoking refresh token: {RefreshToken}", refreshToken);

                var token = await GetRefreshTokenAsync(refreshToken);
                if (token == null)
                {
                    _logger.LogWarning("Attempted to revoke a non-existing or already revoked refresh token: {RefreshToken}", refreshToken);
                    throw new KeyNotFoundException("Refresh token not found or has been revoked.");
                }

                token.is_revoked = true;
                token.revoked_at = DateTime.Now;
                _dbContext.refresh_tokens.Update(token);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully revoked refresh token: {RefreshToken}", refreshToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error occurred while revoking refresh token: {RefreshToken}", refreshToken);
                throw new InvalidOperationException("An error occurred while revoking the refresh token.", ex);
            }
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