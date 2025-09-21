using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IRefreshTokenRepository _tokenRepository;
        private readonly ILogger<AuthService> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthService class with the specified services and repositories.
        /// </summary>
        public AuthService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IRefreshTokenRepository tokenRepository, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _tokenRepository = tokenRepository;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user asynchronously by verifying their credentials and generating a JWT token if successful
        /// </summary>
        /// <param name="loginRequest">The login request containing user identifier (email or phone) and password.</param>
        /// <returns>An AuthResult indicating whether authentication was successful, including a JWT token if successful.</returns>
        public async Task<ServiceResponse<AuthResult>> AuthenticateAsync(LoginRequest loginRequest)
        {
            _logger.LogInformation("Authentication started for {EmailOrPhone}", loginRequest.EmailOrPhone);

            try
            {
                // Retrieve the user by their email or phone identifier from the request
                var user = await _userRepository.FindByEmailOrPhoneAsync(loginRequest.EmailOrPhone);

                // Check if the user exists
                if (user == null)
                {
                    _logger.LogWarning("Authentication failed for {EmailOrPhone}: User not found", loginRequest.EmailOrPhone);
                    return ServiceResponse<AuthResult>.ErrorResponse(ErrorMessages.InvalidCredentials, 401);
                }

                // Check if the user's status is active
                if (!user.status.Equals(UserStatus.active))
                {
                    _logger.LogWarning("Authentication failed for {EmailOrPhone}: User is not active", loginRequest.EmailOrPhone);
                    return ServiceResponse<AuthResult>.ErrorResponse("User is not allowed to log in. The account is not active.", 403);
                }

                // Check if the provided password matches the stored password hash
                // If the password verification fails, return an unsuccessful authentication result
                if (!VerifyHashPassword(loginRequest.Password, user.password_hash, user.password_salt))
                {
                    _logger.LogWarning("Authentication failed for {EmailOrPhone}: Invalid password", loginRequest.EmailOrPhone);
                    return ServiceResponse<AuthResult>.ErrorResponse(ErrorMessages.InvalidCredentials, 401);
                }

                // Generate JWT token with embedded user information
                // Generate JWT token (short-lived)
                var token = _jwtTokenGenerator.GenerateToken(user);

                // Generate Refresh Token (longer-lived)
                var refreshToken = GenerateRefreshToken();

                // Save Refresh Token to the database
                await _tokenRepository.SaveRefreshTokenAsync(user.user_id, refreshToken);

                _logger.LogInformation("Authentication successful for {EmailOrPhone}", loginRequest.EmailOrPhone);

                // Return the result with the generated token and success status
                return ServiceResponse<AuthResult>.SuccessResponse(new AuthResult { Token = token, RefreshToken = refreshToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during authentication for {EmailOrPhone}", loginRequest.EmailOrPhone);
                return ServiceResponse<AuthResult>.ErrorResponse(ErrorMessages.ServerDown, 503);
            }
        }

        private string GenerateRefreshToken()
        {
            // Generate a secure refresh token
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public async Task<ServiceResponse<AuthResult>> RefreshTokenAsync(string refreshToken)
        {
            _logger.LogInformation("Refreshing token for {RefreshToken}", refreshToken);

            try
            {
                var tokenEntity = await _tokenRepository.GetRefreshTokenAsync(refreshToken);

                if (tokenEntity == null || tokenEntity.expiration_time < DateTime.Now)
                {
                    _logger.LogWarning("Token refresh failed: Invalid or expired refresh token {RefreshToken}", refreshToken);
                    //return ServiceResponse<AuthResult>.ErrorResponse("Token refresh failed. Invalid or expired refresh token.");
                    return ServiceResponse<AuthResult>.ErrorResponse("Token refresh failed. Invalid or expired refresh token.", 401);
                }

                var user = await _userRepository.GetByIdAsync(tokenEntity.user_id);
                if (user == null)
                {
                    _logger.LogWarning("User not found for refresh token {RefreshToken}", refreshToken);
                    return ServiceResponse<AuthResult>.ErrorResponse("User not found.", 404);
                }

                // Generate a new JWT token
                var newAccessToken = _jwtTokenGenerator.GenerateToken(user!);

                // Optionally: Generate a new refresh token
                var newRefreshToken = GenerateRefreshToken();

                // Revoke the old refresh token
                await _tokenRepository.RevokeRefreshTokenAsync(refreshToken);

                // Save the new refresh token
                await _tokenRepository.SaveRefreshTokenAsync(user!.user_id, newRefreshToken);

                _logger.LogInformation("Token refresh successful for user {UserId}", user.user_id);

                return ServiceResponse<AuthResult>.SuccessResponse(new AuthResult
                {
                    Success = true,
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while refreshing token {RefreshToken}", refreshToken);
                return ServiceResponse<AuthResult>.ErrorResponse(ErrorMessages.OperationFailed);
            }
        }

        public async Task<ServiceResponse<bool>> RevokeRefreshTokenAsync(string refreshToken)
        {
            _logger.LogInformation("Revoking refresh token {RefreshToken}", refreshToken);

            var tokenEntity = await _tokenRepository.GetRefreshTokenAsync(refreshToken);
            if (tokenEntity == null)
            {
                _logger.LogWarning("Refresh token not found {RefreshToken}", refreshToken);
                return ServiceResponse<bool>.ErrorResponse("Refresh token not found.", 404);
            }

            await _tokenRepository.RevokeRefreshTokenAsync(refreshToken);
            return ServiceResponse<bool>.SuccessResponse(true);
        }

        /// <summary>
        /// Verifies if the provided password matches the stored password hash using the provided salt.
        /// </summary>
        /// <param name="password">The plain text password to verify.</param>
        /// <param name="passwordHash">The stored hash of the password for comparison.</param>
        /// <param name="passwordSalt">The salt used when hashing the stored password.</param>
        /// <returns>True if the password matches the stored hash; otherwise, false.</returns>
        public bool VerifyHashPassword(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            // Create a new instance of HMACSHA512 with the provided salt
            using (var h = new HMACSHA512(passwordSalt))
            {
                // Compute the hash of the provided password using the same hashing algorithm and salt
                var hash = h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                // Compare the computed hash with the stored password hash
                // If they match, return true indicating the password is correct
                return hash.SequenceEqual(passwordHash);
            }
        }
    }
}