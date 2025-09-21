using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using OmmoBackend.Models;

namespace OmmoBackend.Services.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
        string GeneratePasswordResetToken(
                string secret,
                string issuer,
                string audience,
                Dictionary<string, string> claims,
                DateTimeOffset notBefore,
                DateTimeOffset expires);

        ClaimsPrincipal? ValidateToken(string token);
    }
}