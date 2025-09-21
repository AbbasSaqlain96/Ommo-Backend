using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using Twilio.Http;
using Twilio.TwiML.Voice;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace OmmoBackend.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IPasswordService _passwordService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UserService> _logger;
        private readonly IFileStorageService _fileStorageService;

        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ISendEmailRepository _sendEmailRepository;
        private readonly ISystemClock _clock;

        /// <summary>
        /// Initializes a new instance of the UserService class with the specified repositories.
        /// </summary>
        public UserService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            ICompanyRepository companyRepository,
            IPasswordService passwordService,
            IWebHostEnvironment environment,
            ILogger<UserService> logger,
            IFileStorageService fileStorageService,
            IEmailService emailService,
            IConfiguration config,
            ISendEmailRepository sendEmailRepository,
            ISystemClock clock,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _companyRepository = companyRepository;
            _passwordService = passwordService;
            _environment = environment;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _emailService = emailService;
            _config = config;
            _sendEmailRepository = sendEmailRepository;
            _clock = clock;
            _jwtTokenGenerator = jwtTokenGenerator;
        }


        public async Task<ServiceResponse<CreateUserSignupResult>> CreateUserSignupAsync(CreateUserSignupRequest createUserSignupRequest, string? profileImageUrl = null)
        {
            _logger.LogInformation("Starting user signup process for email: {Email}", createUserSignupRequest.Email);

            try
            {
                // Validate company
                var companyExists = await _companyRepository.ExistsAsync(createUserSignupRequest.CompanyId);

                if (!companyExists)
                {
                    return ServiceResponse<CreateUserSignupResult>.ErrorResponse("Company not found. Please provide a valid Company ID", 400);
                }

                // Check if a user with the same email or phone already exists
                var existingUser = await _userRepository.FindByEmailOrPhoneAsync(createUserSignupRequest.Email, createUserSignupRequest.Phone);

                if (existingUser != null)
                {
                    _logger.LogWarning("User with email {Email} or phone {Phone} already exists.", createUserSignupRequest.Email, createUserSignupRequest.Phone);
                    return ServiceResponse<CreateUserSignupResult>.ErrorResponse("A user with the same email or phone already exists.", 400);
                }

                // Validate role existence and company association
                var roleExists = await _roleRepository.RoleExistsAsync(createUserSignupRequest.RoleId);

                if (!roleExists)
                {
                    return ServiceResponse<CreateUserSignupResult>.ErrorResponse("Invalid Role. The specified Role ID either does not exist.");
                }

                // Validate status
                var allowedStatuses = new[] { "active", "in_active" };
                if (!allowedStatuses.Contains(createUserSignupRequest.Status?.ToLower()))
                {
                    return ServiceResponse<CreateUserSignupResult>.ErrorResponse("Invalid status. Allowed values are 'active' or 'in_active'.", 400);
                }

                // Hash the user's password
                _passwordService.HashPassword(createUserSignupRequest.Password, out byte[] passwordHash, out byte[] passwordSalt);

                if (createUserSignupRequest.Email == null)
                    createUserSignupRequest.Email = null;

                if (createUserSignupRequest.Phone == null)
                    createUserSignupRequest.Phone = null;

                if (profileImageUrl == null)
                    profileImageUrl = null;

                // Create a new User object
                var user = new User
                {
                    user_name = createUserSignupRequest.Username,
                    user_email = createUserSignupRequest.Email,
                    phone = createUserSignupRequest.Phone,
                    password_hash = passwordHash,
                    password_salt = passwordSalt,
                    company_id = createUserSignupRequest.CompanyId,
                    role_id = createUserSignupRequest.RoleId,
                    profile_image_url = profileImageUrl,
                    status = UserStatus.active
                };

                // Add the new user to the repository
                await _userRepository.AddAsync(user);

                _logger.LogInformation("User {UserId} created successfully.", user.user_id);

                return ServiceResponse<CreateUserSignupResult>.SuccessResponse(new CreateUserSignupResult
                {
                    UserId = user.user_id,
                    EmailOrPhone = !string.IsNullOrWhiteSpace(createUserSignupRequest.Email) ? createUserSignupRequest.Email : createUserSignupRequest.Phone,
                    Password = createUserSignupRequest.Password,
                    PasswordHash = Convert.ToBase64String(passwordHash),
                    PasswordSalt = Convert.ToBase64String(passwordSalt)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateUserSignupAsync.");
                // Return failure result
                return ServiceResponse<CreateUserSignupResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        /// <summary>
        /// Creates a new user asynchronously.
        /// </summary>
        /// <param name="createUserRequest">The request data for creating a user.</param>
        /// <returns>A UserCreationResult indicating the outcome of the user creation operation.</returns>
        public async Task<ServiceResponse<UserCreationResult>> CreateUserAsync(CreateUserRequest createUserRequest, int companyId)
        {
            try
            {
                _logger.LogInformation("Starting user creation process for email/phone");

                // Check if a user with the same email or phone already exists
                var duplicateCheckResultForUserEntity = await CheckDuplicateEmailAndPhoneInUserAsync(createUserRequest.Email, createUserRequest.Phone);
                if (duplicateCheckResultForUserEntity.HasDuplicate)
                    return ServiceResponse<UserCreationResult>.ErrorResponse(duplicateCheckResultForUserEntity.Message!, 400);

                // Validate the role ID and check the role's company association
                var role = await _roleRepository.GetRoleByIdAsync(createUserRequest.RoleId);

                if (role == null || (!string.Equals(role.role_cat.ToString(), "standard", StringComparison.OrdinalIgnoreCase) && role.company_id != companyId))
                {
                    _logger.LogWarning("Invalid or unauthorized role. RoleId: {RoleId}, CompanyId: {CompanyId}", createUserRequest.RoleId, companyId);
                    return ServiceResponse<UserCreationResult>.ErrorResponse("Invalid role", 400);
                }

                // Hash the user's password
                _passwordService.HashPassword(createUserRequest.Password, out byte[] passwordHash, out byte[] passwordSalt);

                // Create a new User object
                var user = new User
                {
                    user_name = createUserRequest.Username,
                    user_email = createUserRequest.Email,
                    phone = createUserRequest.Phone,
                    password_hash = passwordHash,
                    password_salt = passwordSalt,
                    company_id = companyId,
                    role_id = createUserRequest.RoleId,
                    profile_image_url = null,
                    status = UserStatus.active
                };

                // Add the new user to the repository
                await _userRepository.AddAsync(user);

                _logger.LogInformation("User created successfully. UserId: {UserId}", user.user_id);
                return ServiceResponse<UserCreationResult>.SuccessResponse(new UserCreationResult { Success = true, UserId = user.user_id }, "User created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected server error during user creation.");
                // Return failure result
                return ServiceResponse<UserCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<string> UpdateProfileImageAsync(int userId, string profileImagePath)
        {
            _logger.LogInformation("Updating profile image for user ID: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                throw new Exception("User not found");
            }

            user.profile_image_url = profileImagePath;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Profile image updated successfully for user ID: {UserId}", userId);
            return user.profile_image_url;
        }


        public async Task<ServiceResponse<UserUpdateResult>> UpdateUserAsync(UpdateUserRequest updateUserRequest)
        {
            try
            {
                _logger.LogInformation("Updating user with ID: {UserId}", updateUserRequest.UserId);

                // Check if the user exists
                var user = await _userRepository.GetByIdAsync(updateUserRequest.UserId);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", updateUserRequest.UserId);
                    return ServiceResponse<UserUpdateResult>.ErrorResponse("Invalid User. The provided User ID does not exist", 400);
                }

                // Validate the role id if provided
                if (updateUserRequest.RoleId.HasValue)
                {
                    var role = await _roleRepository.GetByIdAsync(updateUserRequest.RoleId.Value);

                    if (role == null)
                    {
                        _logger.LogWarning("Invalid role ID: {RoleId} for user ID: {UserId}", updateUserRequest.RoleId.Value, updateUserRequest.UserId);
                        return ServiceResponse<UserUpdateResult>.ErrorResponse("Invalid Role. The specified Role ID either does not exist", 400);
                    }

                    // Check if the role is not 'standard', and validate the company association
                    if (!string.Equals(role.role_cat.ToString(), "standard", StringComparison.OrdinalIgnoreCase) && role.company_id != user.company_id)
                    {
                        _logger.LogWarning("Role ID {RoleId} does not belong to the same company as user ID {UserId}", updateUserRequest.RoleId.Value, updateUserRequest.UserId);
                        return ServiceResponse<UserUpdateResult>.ErrorResponse("Invalid Role. The specified Role ID either does not exist", 400);
                    }
                }

                if (!string.IsNullOrWhiteSpace(updateUserRequest.Email))
                {
                    bool emailExists = await _userRepository.CheckIfEmailExists(updateUserRequest.Email, updateUserRequest.UserId);
                    if (emailExists)
                        return ServiceResponse<UserUpdateResult>.ErrorResponse("User Update failed: A user with the same email associated with another user", 400);

                    user.user_email = updateUserRequest.Email;
                }

                if (!string.IsNullOrWhiteSpace(updateUserRequest.Phone))
                {
                    bool phoneExists = await _userRepository.CheckIfPhoneExists(updateUserRequest.Phone, updateUserRequest.UserId);
                    if (phoneExists)
                        return ServiceResponse<UserUpdateResult>.ErrorResponse("User Update failed: A user with the same phone associated with another user", 400);

                    user.phone = updateUserRequest.Phone;
                }

                // Update user fields if values are provided; ignore nulls
                if (!string.IsNullOrWhiteSpace(updateUserRequest.Username))
                {
                    _logger.LogDebug("Updating username for user ID: {UserId}", updateUserRequest.UserId);
                    user.user_name = updateUserRequest.Username;
                }

                if (updateUserRequest.RoleId.HasValue)
                {
                    _logger.LogDebug("Updating role ID for user ID: {UserId}", updateUserRequest.UserId);
                    user.role_id = updateUserRequest.RoleId.Value;
                }

                // Update the user in the database
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User with ID {UserId} updated successfully", updateUserRequest.UserId);
                return ServiceResponse<UserUpdateResult>.SuccessResponse(new UserUpdateResult { Success = true }, "User updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating user.");
                return ServiceResponse<UserUpdateResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }


        /// <summary>
        /// Updates an existing user asynchronously.
        /// </summary>
        /// <param name="updateUserRequest">The request data for updating a user.</param>
        /// <returns>A UserUpdateResult indicating the outcome of the update user operation.</returns>
        public async Task<ServiceResponse<UserUpdateResult>> UpdateUserAsync(UpdateUserRequest updateUserRequest, string profileImageUrl, HttpRequest request)
        {
            _logger.LogInformation("Updating user with ID: {UserId}", updateUserRequest.UserId);

            // Check if the user exists
            var user = await _userRepository.GetByIdAsync(updateUserRequest.UserId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", updateUserRequest.UserId);
                return ServiceResponse<UserUpdateResult>.ErrorResponse("User not found.");
            }

            // Validate the role id if provided
            if (updateUserRequest.RoleId.HasValue)
            {
                var role = await _roleRepository.GetByIdAsync(updateUserRequest.RoleId.Value);

                if (role == null)
                {
                    _logger.LogWarning("Invalid role ID: {RoleId} for user ID: {UserId}", updateUserRequest.RoleId.Value, updateUserRequest.UserId);
                    return ServiceResponse<UserUpdateResult>.ErrorResponse("Invalid role.");
                }

                // Check if the role is not 'standard', and validate the company association
                if (!string.Equals(role.role_cat.ToString(), "standard", StringComparison.OrdinalIgnoreCase))
                {
                    if (role.company_id != user.company_id)
                    {
                        _logger.LogWarning("Role ID {RoleId} does not belong to the same company as user ID {UserId}", updateUserRequest.RoleId.Value, updateUserRequest.UserId);
                        return ServiceResponse<UserUpdateResult>.ErrorResponse("Role does not belong to the same company as the user being created.");
                    }
                }
            }

            // Update user fields if values are provided; ignore nulls
            if (!string.IsNullOrWhiteSpace(updateUserRequest.Username))
            {
                _logger.LogDebug("Updating username for user ID: {UserId}", updateUserRequest.UserId);
                user.user_name = updateUserRequest.Username;
            }

            if (!string.IsNullOrWhiteSpace(updateUserRequest.Email))
            {
                bool emailExists = await _userRepository.CheckIfEmailExists(updateUserRequest.Email, updateUserRequest.UserId);
                if (emailExists)
                    return ServiceResponse<UserUpdateResult>.ErrorResponse("User Update failed: A user with the same email associated with another user", 400);

                user.user_email = updateUserRequest.Email;
            }

            if (!string.IsNullOrWhiteSpace(updateUserRequest.Phone))
            {
                bool phoneExists = await _userRepository.CheckIfPhoneExists(updateUserRequest.Phone, updateUserRequest.UserId);
                if (phoneExists)
                    return ServiceResponse<UserUpdateResult>.ErrorResponse("User Update failed: A user with the same phone associated with another user", 400);

                user.phone = updateUserRequest.Phone;
            }

            if (updateUserRequest.RoleId.HasValue)
            {
                _logger.LogDebug("Updating role ID for user ID: {UserId}", updateUserRequest.UserId);
                user.role_id = updateUserRequest.RoleId.Value;
            }

            // Update the user's profile image URL if a new image was provided
            if (!string.IsNullOrEmpty(profileImageUrl))
            {
                _logger.LogDebug("Updating profile image for user ID: {UserId}", updateUserRequest.UserId);
                // Optional: delete the old profile image if you want to free up space
                var oldProfileImageUrl = user.profile_image_url;
                if (!string.IsNullOrEmpty(oldProfileImageUrl))
                {
                    // Extract relative path
                    var relativePath = oldProfileImageUrl.Replace($"{request.Scheme}://{request.Host}/", string.Empty);

                    // Combine with the current directory to get the full local path
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

                    if (File.Exists(oldFilePath))
                        File.Delete(oldFilePath);
                }
                // Update with the new image URL
                user.profile_image_url = profileImageUrl;
            }

            try
            {
                // Update the user in the database
                await _userRepository.UpdateAsync(user);
                _logger.LogInformation("User with ID {UserId} updated successfully", updateUserRequest.UserId);
                return ServiceResponse<UserUpdateResult>.SuccessResponse(new UserUpdateResult { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating user ID: {UserId}", updateUserRequest.UserId);
                return ServiceResponse<UserUpdateResult>.ErrorResponse("An error occurred while updating the user. Please try again later.");
            }
        }

        /// <summary>
        /// Checks if a user belongs to a specified company asynchronously.
        /// </summary>
        /// <param name="userId">The Id of the user to validate.</param>
        /// <param name="companyId">The Id of the company to check against.</param>
        /// <returns>A boolean indicating whether the user belongs to the specified company.</returns>
        public async Task<bool> UserBelongsToCompanyAsync(int userId, int companyId)
        {
            _logger.LogDebug("Checking if user ID: {UserId} belongs to company ID: {CompanyId}", userId, companyId);
            // Validate that the user belongs to the company
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null && user.company_id == companyId;
        }

        public async Task<ServiceResponse<UserDto>> GetUserByIdAsync(int userId)
        {
            _logger.LogInformation("Fetching user details for user ID: {UserId}", userId);

            try
            {
                // Fetch the user information from the repository
                var user = await _userRepository.GetUserByIdAsync(userId);

                // Return the user object if found, otherwise return null
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    return ServiceResponse<UserDto>.ErrorResponse("User not found", 404);
                }

                _logger.LogInformation("Successfully retrieved user details for user ID: {UserId}", userId);
                return ServiceResponse<UserDto>.SuccessResponse(user, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching user details for user ID: {UserId}", userId);
                return ServiceResponse<UserDto>.ErrorResponse("An error occurred while fetching the user details", 503);
            }
        }

        public async Task<bool> CheckIfEmailOrPhoneExistsAsync(string email, string phone)
        {
            _logger.LogInformation("Checking if email or phone exists: Email={Email}, Phone={Phone}", email, phone);
            // Check if the email or phone exists in the user table
            return await _userRepository.CheckDuplicateEmailOrPhoneAsync(email, phone);
        }

        public async Task<ServiceResponse<string>> UpdateUserPasswordAsync(string identifier, string newPassword)
        {
            _logger.LogInformation("Updating password for user with identifier: {Identifier}", identifier);

            try
            {
                // Fetch user by identifier (email or phone)
                var user = await _userRepository.FindByEmailOrPhoneAsync(identifier);

                if (user == null)
                {
                    _logger.LogWarning("User not found for identifier: {Identifier}", identifier);
                    return ServiceResponse<string>.ErrorResponse("Identifier not found. Please check the provided email or phone number.", 404);
                }

                // Hash the new password
                _passwordService.HashPassword(newPassword, out byte[] passwordHash, out byte[] passwordSalt);

                user.password_hash = passwordHash;
                user.password_salt = passwordSalt;

                // Update the user in the database
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Password updated successfully for user with identifier: {Identifier}", identifier);
                return ServiceResponse<string>.SuccessResponse(null, "Password updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the password for user with identifier: {Identifier}", identifier);
                return ServiceResponse<string>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<int> GetCompanyIdByUserId(int userId)
        {
            _logger.LogInformation("Retrieving company ID for user ID: {UserId}", userId);

            try
            {
                int? companyId = await _userRepository.GetCompanyId(userId);

                // Validate if the company ID exists
                if (companyId == null)
                {
                    _logger.LogWarning("No company ID found for user with ID: {UserId}", userId);
                    throw new KeyNotFoundException($"No company ID found for user with ID: {userId}");
                }

                _logger.LogInformation("Company ID {CompanyId} retrieved for user ID: {UserId}", companyId.Value, userId);
                return companyId.Value;
            }
            catch (KeyNotFoundException knfEx)
            {
                _logger.LogError(knfEx, "KeyNotFoundException: {Message}", knfEx.Message);
                // Rethrow or return a default value depending on business requirements
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the company ID for user ID: {UserId}", userId);
                // Rethrow the exception or handle it as per your application's needs
                throw new ApplicationException("An error occurred while retrieving the company ID.", ex);
            }
        }

        public async Task<ServiceResponse<UserUpdateResult>> ToggleUserStatusAsync(int userId, int companyId)
        {
            _logger.LogInformation("Toggling status for user {UserId} in company {CompanyId}", userId, companyId);

            try
            {
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null || user.company_id != companyId)
                {
                    _logger.LogWarning("Invalid or unauthorized access for user ID {UserId} and company ID {CompanyId}", userId, companyId);
                    return ServiceResponse<UserUpdateResult>.ErrorResponse("Invalid User. The provided User ID does not exist", 400);
                }

                user.status = user.status == UserStatus.active ? UserStatus.in_active : UserStatus.active;

                // Update the user status in the database
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Successfully toggled status for user {UserId}", userId);
                return ServiceResponse<UserUpdateResult>.SuccessResponse(new UserUpdateResult { Success = true }, "User status updated successfully.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access error for user {UserId}", userId);
                return ServiceResponse<UserUpdateResult>.ErrorResponse("You do not have permission to access this resource", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while toggling status for user {UserId}", userId);
                return ServiceResponse<UserUpdateResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<IEnumerable<UserRoleDto>>> GetUsersByCompanyAsync(int companyId)
        {
            _logger.LogInformation("Fetching users for company {CompanyId}", companyId);

            try
            {
                var users = await _userRepository.GetUsersByCompanyIdAsync(companyId);

                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users found for company ID {CompanyId}", companyId);
                    return ServiceResponse<IEnumerable<UserRoleDto>>.ErrorResponse("No users found for the provided company Id.", 404);
                }

                _logger.LogInformation("Users retrieved successfully for company {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<UserRoleDto>>.SuccessResponse(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users for company {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<UserRoleDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<bool>> UpdateMyselfAsync(int userId, UpdateMyselfDto updateDto, IFormFile? profileImageUrl)
        {
            var companyId = await GetCompanyIdByUserId(userId);
            if (companyId == null)
                return ServiceResponse<bool>.ErrorResponse("User not found", 404);

            bool pictureUpdated = false;
            bool detailsUpdated = false;

            // 1. Handle delete picture
            if (updateDto?.DeletePicture == true)
            {
                await _fileStorageService.DeleteProfileImageAsync(userId);
                pictureUpdated = true;
            }

            // 2. Handle new picture upload
            if (profileImageUrl != null && profileImageUrl.Length > 0)
            {
                if (!ValidationHelper.IsValidImageFormat(profileImageUrl, new[] { ".jpeg", ".jpg", ".png", ".webp" }))
                {
                    return ServiceResponse<bool>.ErrorResponse(
                        "Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.",
                        400
                    );
                }

                var uploadResult = await _fileStorageService.UpdateProfileImageAsync(profileImageUrl, companyId, userId);
                if (uploadResult == null)
                    return ServiceResponse<bool>.ErrorResponse("Failed to update profile image.", 500);

                pictureUpdated = true;
            }

            // 3. Update other user details ONLY if needed
            if (updateDto != null && HasDetailsToUpdate(updateDto))
            {
                detailsUpdated = await _userRepository.UpdateUserDetailsAsync(userId, updateDto);
                if (!detailsUpdated)
                    return ServiceResponse<bool>.ErrorResponse("Failed to update user details.", 500);
            }

            // 4. Success if at least one thing was updated
            if (pictureUpdated || detailsUpdated)
                return ServiceResponse<bool>.SuccessResponse(true);

            return ServiceResponse<bool>.ErrorResponse("No changes detected to update.", 400);
        }

        public async Task<ServiceResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            // 1. Match check
            if (request.Password != request.ConfirmPassword)
                return ServiceResponse<string>.ErrorResponse("Passwords do not match.", 400);

            // 2. Strength check
            var (isValid, message) = ValidatePassword(request.Password);
            if (!isValid)
                return ServiceResponse<string>.ErrorResponse(message, 400);

            // 3. Generate hash & salt
            _passwordService.HashPassword(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            // 4. Update in DB
            var updated = await _userRepository.UpdatePasswordAsync(userId, passwordHash, passwordSalt);
            if (!updated)
                return ServiceResponse<string>.ErrorResponse("Failed to update password.", 500);

            return ServiceResponse<string>.SuccessResponse(null, "Password updated successfully.");
        }

        // ========= 1) Request Reset Link =========
        public async Task<ServiceResponse<string>> RequestPasswordResetAsync(string email)
        {
            try
            {
                // 1. Validate user exists
                var user = await _userRepository.GetActiveUserByEmailAsync(email);
                if (user == null)
                    return ServiceResponse<string>.ErrorResponse("Email not associated with an active user.", 404);

                // 2. Load configuration
                var minutes = _config.GetValue<int>("Auth:ResetTokenExpiryMinutes", 20);
                var secret = _config.GetValue<string>("Auth:ResetTokenSecret");
                var issuer = _config["Auth:Issuer"];
                var audience = _config["Auth:Audience"];
                var clientBaseUrl = _config["AppSettings:ClientBaseUrl"];

                // 3. Validate critical config values
                if (string.IsNullOrWhiteSpace(secret))
                    return ServiceResponse<string>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);

                if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(clientBaseUrl))
                    return ServiceResponse<string>.ErrorResponse("Server configuration error. Please contact support.", 503);

                // 4. Validate secret key length for HS256 (>= 32 bytes)
                var keyBytes = Encoding.UTF8.GetBytes(secret);
                if (keyBytes.Length < 32)
                    return ServiceResponse<string>.ErrorResponse("Server configuration error: secret key too short.", 500);

                // 5. Build JWT
                var now = _clock.UtcNow;
                var jti = Guid.NewGuid().ToString("N");

                var token = _jwtTokenGenerator.GeneratePasswordResetToken(
                    secret: secret,
                    issuer: issuer,
                    audience: audience,
                    claims: new Dictionary<string, string>
                    {
                { "user_id", user.user_id.ToString() },
                { JwtRegisteredClaimNames.Jti, jti }
                    },
                    notBefore: now,
                    expires: now.AddMinutes(minutes)
                );

                // 6. Build reset link
                var resetUrl = $"{clientBaseUrl}/update-password?token={token}";

                // 7. Queue email in DB
                var emailId = await _sendEmailRepository.InsertAsync(new SendEmail
                {
                    send_to = email,
                    subject = "Reset your password",
                    status = "queued",
                    created_at = now.UtcDateTime
                });

                // 8. Attempt to send email
                try
                {
                    await _emailService.SendAsync(email, "Reset your password",
                        $"Click the link to reset your password: {resetUrl}");
                    await _sendEmailRepository.MarkSentAsync(emailId, now.UtcDateTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reset password email to {Email}", email);
                    var errorMessage = string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
                    await _sendEmailRepository.MarkFailedAsync(emailId, errorMessage);
                }

                // 9. Always respond success to avoid account enumeration
                return ServiceResponse<string>.SuccessResponse(null, "If the provided email is associated with an account, a password reset link has been sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset request");
                return ServiceResponse<string>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        // ========= 2) Confirm Reset Password =========
        public async Task<ServiceResponse<string>> ConfirmPasswordResetAsync(ResetPasswordConfirmDto dto)
        {
            try
            {
                // 1. Match check
                if (dto.Password != dto.ConfirmPassword)
                    return ServiceResponse<string>.ErrorResponse("Passwords do not match.", 400);

                // 2. Strength check
                var (ok, msg) = ValidatePassword(dto.Password);
                if (!ok)
                    return ServiceResponse<string>.ErrorResponse(msg, 400);

                // 3. Validate token & extract claims
                var principal = _jwtTokenGenerator.ValidateToken(dto.Token);
                if (principal == null)
                    return ServiceResponse<string>.ErrorResponse("Invalid or expired token.", 401);

                var userIdClaim = principal.FindFirst("user_id")?.Value;
                if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
                    return ServiceResponse<string>.ErrorResponse("Invalid token payload.", 401);

                // 4. Hash and save new password
                _passwordService.HashPassword(dto.Password, out var hash, out var salt);
                var updated = await _userRepository.UpdatePasswordAsync(userId, hash, salt);
                if (!updated)
                    return ServiceResponse<string>.ErrorResponse("User not found.", 404);

                return ServiceResponse<string>.SuccessResponse(null, "Password updated successfully.");
            }
            catch (SecurityTokenExpiredException)
            {
                return ServiceResponse<string>.ErrorResponse("Token expired.", 401);
            }
            catch (SecurityTokenException)
            {
                return ServiceResponse<string>.ErrorResponse("Invalid token.", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset confirmation");
                return ServiceResponse<string>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 500);
            }
        }

        public async Task<UserCompanyDto?> GetCurrentUserAsync(int userId)
        {
            return await _userRepository.GetCurrentUserAsync(userId);
        }


        // Helper: checks if DTO has actual updatable fields
        private bool HasDetailsToUpdate(UpdateMyselfDto dto)
        {
            return !string.IsNullOrWhiteSpace(dto.Username) || !string.IsNullOrWhiteSpace(dto.Email) ||
                   !string.IsNullOrWhiteSpace(dto.Phone) || dto.Role != null;
        }

        // Password strength validation
        private (bool isValid, string message) ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required.");

            if (password.Length < 8)
                return (false, "Password must be at least 8 characters long.");

            if (!password.Any(char.IsUpper))
                return (false, "Password must contain at least one uppercase letter.");

            if (!password.Any(char.IsLower))
                return (false, "Password must contain at least one lowercase letter.");

            if (!password.Any(char.IsDigit))
                return (false, "Password must contain at least one digit.");

            if (!password.Any(ch => "!@#$%^&*()_+[]{}|;:,.<>?/`~".Contains(ch)))
                return (false, "Password must contain at least one special character.");

            return (true, string.Empty);
        }


        private async Task<DuplicateCheckResult> CheckDuplicateEmailAndPhoneInUserAsync(string email, string? phone)
        {
            //_logger.LogInformation("Checking for duplicate email {Email} and phone {Phone}", email, phone);

            var (isEmailDuplicate, isPhoneDuplicate) = await _userRepository.CheckDuplicateEmailAndPhoneInUserAsync(email, phone);

            if (isEmailDuplicate && isPhoneDuplicate)
            {
                //  _logger.LogWarning("Duplicate email {Email} and phone {Phone} found.", email, phone);
                return new DuplicateCheckResult
                {
                    HasDuplicate = true,
                    Message = "Duplicate email and phone number found."
                };
            }

            if (isEmailDuplicate)
            {
                //_logger.LogWarning("Duplicate email {Email} found.", email);
                return new DuplicateCheckResult
                {
                    HasDuplicate = true,
                    Message = "The email is already registered with another user."
                };
            }

            if (isPhoneDuplicate)
            {
                //_logger.LogWarning("Duplicate phone {Phone} found.", phone);
                return new DuplicateCheckResult
                {
                    HasDuplicate = true,
                    Message = "The phone number is already registered with another user."
                };
            }

            //_logger.LogInformation("No duplicates found for email {Email} and phone {Phone}.", email, phone);
            return new DuplicateCheckResult
            {
                HasDuplicate = false,
                Message = null
            };
        }
    }
}