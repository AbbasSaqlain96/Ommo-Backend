using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UserRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the UserRepository class with the specified database context.
        /// </summary>
        public UserRepository(AppDbContext dbContext, ILogger<UserRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Finds a user by their email or phone number asynchronously.
        /// </summary>
        /// <param name="email">The email of the user to find.</param>
        /// <param name="phone">The phone number of the user to find.</param>
        /// <returns>A Task representing the asynchronous operation, with a User object if found, or null if not.</returns>
        public async Task<User?> FindByEmailOrPhoneAsync(string? email, string? phone)
        {
            _logger.LogInformation("Searching for user with Email: {Email} or Phone: {Phone}", email, phone);

            try
            {
                var user = await _dbContext.users
                                    .Where(u =>
                                        (!string.IsNullOrEmpty(email) && u.user_email == email) || // Match by email if not null/empty
                                        (string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(phone) && u.phone == phone)) // Match by phone if email is null/empty
                                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    _logger.LogInformation("User found with Email: {Email} or Phone: {Phone}", email, phone);
                }
                else
                {
                    _logger.LogWarning("No user found with Email: {Email} or Phone: {Phone}", email, phone);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching for user with Email: {Email} or Phone: {Phone}", email, phone);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a user from the database based on the provided email or phone identifier.
        /// </summary>
        /// <param name="identifier">The email or phone number used to find the user.</param>
        /// <returns>A Task representing the asynchronous operation, with a result of the user if found; otherwise, null.</returns>
        public async Task<User?> FindByEmailOrPhoneAsync(string identifier)
        {
            _logger.LogInformation("Searching for user with identifier: {Identifier}", identifier);

            try
            {
                // Determine if the identifier is an email or phone number
                bool isEmail = identifier.Contains("@"); // Simple check for email format
                bool isPhone = !isEmail; // Simple check if it's numeric (phone number)

                User? user = null;

                if (isEmail)
                {
                    // If it's an email, search by email only, explicitly handling empty string and null
                    user = await _dbContext.users
                        .FirstOrDefaultAsync(u => !string.IsNullOrEmpty(u.user_email) && u.user_email != "" && u.user_email.ToLower() == identifier.ToLower());
                }
                else if (isPhone)
                {
                    // If it's a phone number, search by phone only, ensuring phone is not null or empty
                    user = await _dbContext.users
                        .FirstOrDefaultAsync(u => !string.IsNullOrEmpty(u.phone) && u.phone != "" && u.phone == identifier);
                }

                if (user != null)
                {
                    _logger.LogInformation("User found with identifier: {Identifier}", identifier);
                }
                else
                {
                    _logger.LogWarning("No user found with identifier: {Identifier}", identifier);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching for user with identifier: {Identifier}", identifier);
                // Handle any exceptions here
                throw new Exception("An error occurred while searching for user by email or phone.", ex);
            }
        }


        public async Task<bool> CheckDuplicateEmailOrPhoneAsync(string email, string phone)
        {
            _logger.LogInformation("Checking for duplicate Email: {Email} or Phone: {Phone}", email, phone);

            try
            {
                var exists = await _dbContext.users
                    .AnyAsync(u => u.user_email == email || u.phone == phone);

                if (exists)
                {
                    _logger.LogWarning("Duplicate Email: {Email} or Phone: {Phone} found", email, phone);
                }
                else
                {
                    _logger.LogInformation("No duplicate found for Email: {Email} or Phone: {Phone}", email, phone);
                }

                return exists;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database operation failed while checking duplicate Email: {Email} or Phone: {Phone}", email, phone);
                throw new DataAccessException(ErrorMessages.DatabaseOperationFailed, dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while checking duplicate Email: {Email} or Phone: {Phone}", email, phone);
                throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
            }
        }

        public async Task<bool> CheckIfUserOrCompanyExistsAsync(string receiver, bool isEmail)
        {
            _logger.LogInformation("Checking if user or company exists for Receiver: {Receiver}, IsEmail: {IsEmail}", receiver, isEmail);

            try
            {
                bool exists;

                // Check if the receiver is an email address
                if (isEmail)
                {
                    // Look for any existing user or company with the provided email
                    exists = await _dbContext.users.AnyAsync(u => u.user_email == receiver) ||
                        await _dbContext.company.AnyAsync(c => c.email == receiver);
                }
                else
                {
                    // Look for any existing user or company with the provided phone number
                    exists = await _dbContext.users.AnyAsync(u => u.phone == receiver) ||
                        await _dbContext.company.AnyAsync(c => c.phone == receiver);
                }
                _logger.LogInformation("Existence check result for Receiver: {Receiver} -> {Exists}", receiver, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if user or company exists for Receiver: {Receiver}", receiver);
                throw;
            }
        }

        public async Task<int?> GetCompanyId(int userId)
        {
            _logger.LogInformation("Fetching company ID for User ID: {UserId}", userId);

            try
            {
                var companyId = await _dbContext.users
                      .Where(x => x.user_id == userId)
                      .Select(x => x.company_id)
                      .FirstOrDefaultAsync();

                if (companyId == null)
                {
                    _logger.LogWarning("Company ID not found for User ID: {UserId}", userId);
                    throw new Exception("Company ID not found for the given user ID.");
                }

                _logger.LogInformation("Company ID {CompanyId} retrieved for User ID: {UserId}", companyId, userId);
                return companyId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company ID for User ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Role>> GetUserRolesAsync(int userId, int companyId)
        {
            _logger.LogInformation("Fetching roles for User ID: {UserId} in Company ID: {CompanyId}", userId, companyId);

            try
            {
                // Fetch the user's roles associated with the given company ID
                var userCompanyRoles = await _dbContext.role
                    .Where(r => r.company_id == companyId)
                    .ToListAsync();

                // Fetch all standard roles (company_id is null or 0)
                var standardRoles = await _dbContext.role
                    .Where(r => r.company_id == null || r.company_id == 0)
                    .ToListAsync();

                // Combine the two lists of roles
                var allRoles = userCompanyRoles.Concat(standardRoles).ToList();

                _logger.LogInformation("Retrieved {RoleCount} roles for User ID: {UserId}", allRoles.Count, userId);
                return allRoles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for User ID: {UserId} in Company ID: {CompanyId}", userId, companyId);
                throw;
            }
        }

        public async Task<IEnumerable<UserRoleDto>> GetUsersByCompanyIdAsync(int companyId)
        {
            _logger.LogInformation("Fetching users for Company ID: {CompanyId}", companyId);

            try
            {
                var users = await _dbContext.users
               .Where(u => u.company_id == companyId)
               .Join(
                   _dbContext.role,
                   user => user.role_id,
                   role => role.role_id,
                   (user, role) => new UserRoleDto
                   {
                       UserId = user.user_id,
                       Name = user.user_name,
                       Email = user.user_email,
                       Phone = user.phone,
                       Status = user.status.ToString(),
                       RoleName = role.role_name
                   }
               )
               .ToListAsync();

                _logger.LogInformation("Retrieved {UserCount} users for Company ID: {CompanyId}", users.Count, companyId);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users for Company ID: {CompanyId}", companyId);
                throw new Exception("An error occurred while fetching users. Please try again later.");
            }
        }

        public async Task<bool> CheckIfEmailOrPhoneExists(string emailOrPhone, int userId)
        {
            if (emailOrPhone.Contains("@"))
                return await _dbContext.users.AnyAsync(u => u.user_email == emailOrPhone && u.user_id != userId);
            else
                return await _dbContext.users.AnyAsync(u => u.phone == emailOrPhone && u.user_id != userId);
        }

        public async Task<string?> GetProfileImageUrlAsync(int userId)
        {
            return await _dbContext.users
                 .Where(u => u.user_id == userId)
                 .Select(u => u.profile_image_url)
                 .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateProfileImageUrlAsync(int userId, string? newUrl)
        {
            var user = await _dbContext.users.FindAsync(userId);
            if (user == null)
                return false;

            user.profile_image_url = newUrl;
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateUserDetailsAsync(int userId, UpdateMyselfDto updateDto)
        {
            var user = await _dbContext.users.FindAsync(userId);
            if (user == null) return false;

            user.user_name = updateDto.Username ?? user.user_name;
            user.user_email = updateDto.Email ?? user.user_email;
            user.phone = updateDto.Phone ?? user.phone;
            user.role_id = updateDto.Role ?? user.role_id;

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, byte[] passwordHash, byte[] passwordSalt)
        {
            var user = await _dbContext.users.FirstOrDefaultAsync(u => u.user_id == userId);
            if (user == null)
                return false;

            user.password_hash = passwordHash;
            user.password_salt = passwordSalt;

            return await _dbContext.SaveChangesAsync() > 0;
        }

        public Task<User?> GetActiveUserByEmailAsync(string email) =>
            _dbContext.users.FirstOrDefaultAsync(u => u.user_email == email && u.status == UserStatus.active);

        public async Task<(bool IsEmailDuplicate, bool IsPhoneDuplicate)> CheckDuplicateEmailAndPhoneInUserAsync(string email, string phone)
        {
            try
            {
                _logger.LogInformation("Checking for duplicate email ({Email}) and phone ({Phone}).", email, phone);

                bool isEmailDuplicate = false;
                bool isPhoneDuplicate = false;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    // Check if the email already exists in the database
                    isEmailDuplicate = await _dbContext.users
                        .AnyAsync(c => c.user_email == email);
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    // Check if the phone number already exists in the database
                    isPhoneDuplicate = await _dbContext.users
                        .AnyAsync(c => c.phone == phone);
                }

                _logger.LogInformation("Duplicate check results - Email: {IsEmailDuplicate}, Phone: {IsPhoneDuplicate}", isEmailDuplicate, isPhoneDuplicate);

                return (isEmailDuplicate, isPhoneDuplicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking for duplicate email ({Email}) or phone ({Phone}).", email, phone);
                throw new Exception("An error occurred while checking for duplicate email or phone.", ex);
            }
        }

        public async Task<bool> CheckIfEmailExists(string email, int userId)
        {
            return await _dbContext.users.AnyAsync(u => u.user_email == email && u.user_id != userId);
        }

        public async Task<bool> CheckIfPhoneExists(string phone, int userId)
        {
            return await _dbContext.users.AnyAsync(u => u.phone == phone && u.user_id != userId);
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            return await (from u in _dbContext.users
                          join r in _dbContext.role on u.role_id equals r.role_id
                          where u.user_id == userId
                          select new UserDto
                          {
                              Username = u!.user_name,
                              Email = u.user_email,
                              Phone = u.phone,
                              CompanyId = u.company_id,
                              RoleName = r.role_name,
                              ProfileImageUrl = u.profile_image_url
                          })
                  .AsNoTracking()
                  .FirstOrDefaultAsync();
        }

        public async Task<UserCompanyDto?> GetCurrentUserAsync(int userId)
        {
            var query =
                from user in _dbContext.users
                join company in _dbContext.company
                    on user.company_id equals company.company_id
                join carrier in _dbContext.carrier
                    on company.company_id equals carrier.company_id
                where user.user_id == userId
                select new UserCompanyDto
                {
                    UserId = user.user_id,
                    UserName = user.user_name,
                    UserEmail = user.user_email,
                    Phone = user.phone,
                    ProfileImageUrl = user.profile_image_url,
                    RoleId = user.role_id,
                    Status = user.status,

                    CompanyId = company.company_id,
                    CompanyName = company.name,
                    CompanyEmail = company.email,
                    CompanyPhone = company.phone,
                    CompanyAddress = company.address,
                    CompanyType = company.company_type,
                    CompanyStatus = company.status,
                    CompanyDotNumber = company.dot_number,

                    CompanyMCNumber = carrier.mc_number
                };

            return await query.FirstOrDefaultAsync();
        }
    }
}