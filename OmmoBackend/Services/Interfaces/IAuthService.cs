using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResponse<AuthResult>> AuthenticateAsync(LoginRequest loginRequest);
        
        Task<ServiceResponse<AuthResult>> RefreshTokenAsync(string refreshToken);
        
        Task<ServiceResponse<bool>> RevokeRefreshTokenAsync(string refreshToken);
    }
}