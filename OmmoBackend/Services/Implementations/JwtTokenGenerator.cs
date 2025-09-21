using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Models;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtTokenGenerator> _logger;
        public JwtTokenGenerator(IConfiguration configuration, ILogger<JwtTokenGenerator> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generates a JWT token for the specified user.
        /// </summary>
        /// <param name="user">The user for whom the JWT token is to be generated.</param>
        /// <returns>A JWT token as a string.</returns>
        public string GenerateToken(User user)
        {
            try
            {
                _logger.LogInformation("Generating JWT token for User ID: {UserId}, Company ID: {CompanyId}, Role ID: {RoleId}", user.user_id, user.company_id, user.role_id);

                // Create a security key based on the secret key from configuration
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Define the claims to be included in the token
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.user_id.ToString()), // Include the user ID with the correct claim type
                    new Claim("Company_ID", user.company_id.ToString()),
                    new Claim("Role_ID", user.role_id.ToString())
                };

                // Create the JWT token with issuer, audience, claims, expiration, and signing credentials
                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(7),
                    signingCredentials: creds
                );

                // Convert the JWT token to a string
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                _logger.LogInformation("JWT token generated successfully for User ID: {UserId}", user.user_id);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for User ID: {UserId}", user.user_id);
                throw new Exception(ErrorMessages.OperationFailed, ex);
            }
        }

        public string GeneratePasswordResetToken(
            string secret,
            string issuer,
            string audience,
            Dictionary<string, string> claims,
            DateTimeOffset notBefore,
            DateTimeOffset expires)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetBytes(secret).Length < 32)
                {
                    throw new InvalidOperationException("Reset token secret must be at least 32 bytes for HS256.");
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var tokenClaims = claims.Select(c => new Claim(c.Key, c.Value)).ToList();

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: tokenClaims,
                    notBefore: notBefore.UtcDateTime,
                    expires: expires.UtcDateTime,
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token");
                throw;
            }
        }


        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Auth:ResetTokenSecret"]);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }
    }
}