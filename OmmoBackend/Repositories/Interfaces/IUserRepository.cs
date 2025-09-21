using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> FindByEmailOrPhoneAsync(string? email, string? phone);
        Task<User> FindByEmailOrPhoneAsync(string identifier);
        Task<bool> CheckDuplicateEmailOrPhoneAsync(string email, string phone);
        Task<bool> CheckIfUserOrCompanyExistsAsync(string receiver, bool isEmail);
        Task<int?> GetCompanyId(int userId);
        Task<List<Role>> GetUserRolesAsync(int userId, int companyId);
        Task<IEnumerable<UserRoleDto>> GetUsersByCompanyIdAsync(int companyId);
        Task<bool> CheckIfEmailOrPhoneExists(string emailOrPhone, int userId);

        Task<string?> GetProfileImageUrlAsync(int userId);
        Task<bool> UpdateProfileImageUrlAsync(int userId, string? newUrl);
        Task<bool> UpdateUserDetailsAsync(int userId, UpdateMyselfDto updateDto);
        Task<bool> UpdatePasswordAsync(int userId, byte[] passwordHash, byte[] passwordSalt);

        Task<User?> GetActiveUserByEmailAsync(string email);

        Task<(bool IsEmailDuplicate, bool IsPhoneDuplicate)> CheckDuplicateEmailAndPhoneInUserAsync(string email, string phone);

        Task<bool> CheckIfEmailExists(string email, int userId);

        Task<bool> CheckIfPhoneExists(string phone, int userId);

        Task<UserDto> GetUserByIdAsync(int userId);

        Task<UserCompanyDto?> GetCurrentUserAsync(int userId);
    }
}