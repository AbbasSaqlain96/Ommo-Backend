using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResponse<UserCreationResult>> CreateUserAsync(CreateUserRequest createUserRequest, int companyId);
        Task<ServiceResponse<UserUpdateResult>> UpdateUserAsync(UpdateUserRequest updateUserRequest);
        Task<ServiceResponse<UserUpdateResult>> UpdateUserAsync(UpdateUserRequest updateUserRequest, string profileImageUrl, HttpRequest request);
        Task<bool> UserBelongsToCompanyAsync(int userId, int companyId);
        Task<ServiceResponse<UserDto>> GetUserByIdAsync(int userId);
        Task<bool> CheckIfEmailOrPhoneExistsAsync(string email, string phone);

        Task<string> UpdateProfileImageAsync(int userId, string profileImagePath);

        Task<ServiceResponse<string>> UpdateUserPasswordAsync(string identifier, string newPassword);

        Task<int> GetCompanyIdByUserId(int userId);

        Task<ServiceResponse<CreateUserSignupResult>> CreateUserSignupAsync(CreateUserSignupRequest createUserSignupRequest, string? profileImageUrl);

        Task<ServiceResponse<UserUpdateResult>> ToggleUserStatusAsync(int userId, int companyId);
        Task<ServiceResponse<IEnumerable<UserRoleDto>>> GetUsersByCompanyAsync(int companyId);

        Task<ServiceResponse<bool>> UpdateMyselfAsync(int userId, UpdateMyselfDto updateDto, IFormFile? profileImageUrl);

        Task<ServiceResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequest request);


        Task<ServiceResponse<string>> RequestPasswordResetAsync(string email);
        Task<ServiceResponse<string>> ConfirmPasswordResetAsync(ResetPasswordConfirmDto dto);

        Task<UserCompanyDto?> GetCurrentUserAsync(int userId);
    }
}