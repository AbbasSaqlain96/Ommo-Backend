using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;
using System.Security.Claims;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuthService _authService;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Initializes a new instance of the UserController class with the specified user service.
        /// </summary>
        public UserController(
            IUserService userService,
            IFileStorageService fileStorageService,
            IAuthService authService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _fileStorageService = fileStorageService;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost]
        [Route("create-user-signup")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUserSignup(
            [FromForm] CreateUserSignupRequest createUserSignupRequest, IFormFile? profileImageUrl)
        {
            // Check if the request model state is valid
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            try
            {
                //// Validate profile image extension
                //if (profileImageUrl != null)
                //{
                //    var extension = Path.GetExtension(profileImageUrl.FileName).ToLower();
                //    if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
                //    {
                //        _logger.LogWarning("Invalid image format: {FileName}", profileImageUrl.FileName);
                //        return ApiResponse.Error("Invalid image format. Only JPEG, PNG, and WEBP formats are allowed.", 400);
                //    }
                //}

                _logger.LogInformation("Creating user signup for Email: {Email}", createUserSignupRequest.Email);

                // Attempt to create the user (without profile image)
                var userCreationResult = await _userService.CreateUserSignupAsync(createUserSignupRequest, null);

                // Return appropriate response based on the result of the user creation
                if (!userCreationResult.Success)
                {
                    _logger.LogWarning("User creation failed: {ErrorMessage}", userCreationResult.ErrorMessage);
                    return ApiResponse.Error("User creation failed: " + userCreationResult.ErrorMessage, userCreationResult.StatusCode);
                }

                // Get the created UserId
                var userId = userCreationResult.Data.UserId;

                _logger.LogInformation("User created successfully with UserId: {UserId}", userId);

                // Save profile image if it exists
                string profileImagePath = string.Empty;

                if (profileImageUrl != null)
                {
                    try
                    {
                        _logger.LogInformation("Uploading profile image for UserId: {UserId}", userId);

                        profileImagePath = await _fileStorageService.SaveProfileImageAsync(profileImageUrl, createUserSignupRequest.CompanyId, userId);

                        // Update the user's profile image path
                        await _userService.UpdateProfileImageAsync(userId, profileImagePath);

                        _logger.LogInformation("Profile image uploaded successfully for UserId: {UserId}", userId);
                    }
                    catch (CustomFileStorageException fileEx)
                    {
                        _logger.LogError(fileEx, "Profile image upload failed for UserId: {UserId}", userId);
                        return ApiResponse.Error("Failed to upload profile picture. Please try again.", 400);
                    }
                }

                var loginRequest = new LoginRequest
                {
                    EmailOrPhone = userCreationResult.Data.EmailOrPhone,
                    Password = userCreationResult.Data.Password
                };

                // Call the authentication method to handle the logic of validating the user's credentials and return token
                var result = await _authService.AuthenticateAsync(loginRequest);

                _logger.LogInformation("User authenticated successfully for UserId: {UserId}", userId);

                return ApiResponse.Success(new { profileImageUrl = profileImagePath, token = result.Data.Token, refreshToken = result.Data.RefreshToken }, "User created successfully.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse.Error(ex.Message, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error occurred while creating user.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost]
        [Route("create-user")]
        [Authorize]
        public async Task<IActionResult> CreateUser(
            [FromForm] CreateUserRequest createUserRequest)
        {
            // Check if the request model state is valid
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            try
            {
                // Get the logged-in user's company ID from claims
                int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");

                if (companyId <= 0)
                {
                    _logger.LogWarning("Invalid Company ID in token.");
                    return ApiResponse.Error("You do not have permission to access this resource", 401);
                }

                _logger.LogInformation("Creating user for Company ID: {CompanyId}", companyId);

                // Attempt to create the user (without profile image)
                var userCreationResult = await _userService.CreateUserAsync(createUserRequest, companyId);

                // Return appropriate response based on the result of the user creation
                if (!userCreationResult.Success)
                {
                    _logger.LogWarning("User creation failed. Error: {ErrorMessage}", userCreationResult.ErrorMessage);
                    return ApiResponse.Error("User creation failed: " + userCreationResult.ErrorMessage, userCreationResult.StatusCode);
                }

                _logger.LogInformation("User created successfully. UserId: {UserId}", userCreationResult.Data.UserId);
                return ApiResponse.Success(userCreationResult.Data, "User created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in CreateUser.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPut]
        [Route("update-user")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserRequest updateUserRequest)
        {
            try
            {
                // Check if the request model state is valid
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .Select(ms => ms.Value.Errors.First().ErrorMessage)
                        .FirstOrDefault();

                    return ApiResponse.Error(firstError, 400);
                }

                // Get the logged-in user's company ID from claims
                int loggedInCompanyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
                if (loggedInCompanyId <= 0)
                {
                    _logger.LogWarning("Invalid Company ID in token.");
                    return ApiResponse.Error("You do not have permission to access this resource", 401);
                }

                _logger.LogInformation("User update initiated by Company ID: {CompanyId} for User ID: {UserId}", loggedInCompanyId, updateUserRequest.UserId);

                // Get the company ID of the user being updated
                int targetUserCompanyId;
                try
                {
                    targetUserCompanyId = await _userService.GetCompanyIdByUserId(updateUserRequest.UserId);
                }
                catch (KeyNotFoundException)
                {
                    _logger.LogWarning("No company found for User ID: {UserId}", updateUserRequest.UserId);
                    return ApiResponse.Error("Invalid User. The provided User ID does not exist", 400);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving company ID for user.");
                    return ApiResponse.Error("Server is temporarily unavailable. Please try again later.", 503);
                }

                // Ensure the user being updated belongs to the same company
                if (loggedInCompanyId != targetUserCompanyId)
                {
                    _logger.LogWarning("Unauthorized update attempt. Company ID: {LoggedInCompanyId} tried to update User ID: {UserId} from Company ID: {TargetUserCompanyId}",
                        loggedInCompanyId, updateUserRequest.UserId, targetUserCompanyId);
                    return ApiResponse.Error("Invalid User. The provided User ID does not exist", 400);
                }

                // Call the service method to update the user and get the result
                var userUpdateResult = await _userService.UpdateUserAsync(updateUserRequest);

                // Check if the update was successful
                if (!userUpdateResult.Success)
                {
                    _logger.LogWarning("User update failed for User ID: {UserId}. Error: {ErrorMessage}", updateUserRequest.UserId, userUpdateResult.ErrorMessage);
                    return ApiResponse.Error(userUpdateResult.ErrorMessage, userUpdateResult.StatusCode);
                }

                _logger.LogInformation("User updated successfully. User ID: {UserId}", updateUserRequest.UserId);
                return ApiResponse.Success(userUpdateResult.Data, "User updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating user.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet]
        [Route("get-user-info")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> GetUser()
        {
            // Retrieve the logged-in user's Id from the JWT claims
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            // Validate if the user Id is present and parse it; return Unauthorized if the user is not authenticated properly
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var loggedInUserId))
            {
                _logger.LogWarning("Unauthorized access attempt. Invalid or missing UserId in token.");
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }

            try
            {
                _logger.LogInformation("Fetching user info for UserId: {UserId}", loggedInUserId);

                // Call the service method to get a user info
                var userResponse = await _userService.GetUserByIdAsync(loggedInUserId);

                if (!userResponse.Success)
                {
                    if (userResponse.StatusCode == 404)
                        return ApiResponse.Error(userResponse.ErrorMessage, 404);

                    // In case of unexpected service error, respond with 503
                    return ApiResponse.Error("Server is temporarily unavailable. Please try again later.", 503);
                }

                _logger.LogInformation("User info retrieved successfully for UserId: {UserId}", loggedInUserId);
                return ApiResponse.Success(userResponse.Data, "User info fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving user info.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("forget-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest request)
        {
            // Check if the request model is valid
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid forget password request received.");
                return ApiResponse.Error("Invalid request. Please check input values.", 400);
            }

            try
            {
                _logger.LogInformation("Processing password reset request for Identifier: {Identifier}", request.Identifier);

                var result = await _userService.UpdateUserPasswordAsync(request.Identifier, request.NewPassword);

                if (!result.Success)
                {
                    _logger.LogWarning("Password reset failed for Identifier: {Identifier}, Reason: {Error}", request.Identifier, result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Password reset successful for Identifier: {Identifier}", request.Identifier);
                return ApiResponse.Success(null, result.Message);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access attempt while trying to reset password for Identifier: {Identifier}", request.Identifier);
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset for Identifier: {Identifier}", request.Identifier);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPatch]
        [Route("toggle-status")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int userId)
        {
            if (userId <= 0)
                return ApiResponse.Error("Invalid User ID.", 400);

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                _logger.LogInformation("Processing user status toggle request for user ID {UserId}, company ID {CompanyId}", userId, companyId);

                // Call the service method to update the user and get the result
                var userUpdateResult = await _userService.ToggleUserStatusAsync(userId, companyId);

                // Check if the update was successful
                if (!userUpdateResult.Success)
                {
                    _logger.LogWarning("User status update failed for user ID {UserId}, Reason: {Error}", userId, userUpdateResult.ErrorMessage);
                    return ApiResponse.Error(userUpdateResult.ErrorMessage, userUpdateResult.StatusCode);
                }

                _logger.LogInformation("User status successfully updated for user ID {UserId}", userId);
                return ApiResponse.Success(userUpdateResult.Data, userUpdateResult.Message ?? "User status updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating user status for user ID {UserId}", userId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet]
        [Route("get-user-by-company")]
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {
            int companyId = TokenHelper.GetCompanyId(User);
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid company ID in JWT token for user {UserId}", User.Identity?.Name);
                return ApiResponse.Error("Invalid Company ID.", 400);
            }

            try
            {
                _logger.LogInformation("Fetching users for company ID {CompanyId}", companyId);

                var serviceResponse = await _userService.GetUsersByCompanyAsync(companyId);

                if (!serviceResponse.Success)
                {
                    _logger.LogWarning("Service error for company ID {CompanyId}: {Message}", companyId, serviceResponse.ErrorMessage);
                    return ApiResponse.Error(serviceResponse.ErrorMessage, serviceResponse.StatusCode);
                }

                if (serviceResponse.Data == null || !serviceResponse.Data.Any())
                {
                    _logger.LogWarning("No users found for company ID {CompanyId}", companyId);
                    return ApiResponse.Error("No users found for the provided company Id.", 404);
                }

                _logger.LogInformation("Returning {Count} users for company ID {CompanyId}", serviceResponse.Data.Count(), companyId);
                return ApiResponse.Success(serviceResponse.Data);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized access attempt for company ID {CompanyId}", TokenHelper.GetCompanyId(User));
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching users for company ID {CompanyId}", TokenHelper.GetCompanyId(User));
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost]
        [Route("update-myself")]
        [Authorize]
        public async Task<IActionResult> UpdateMyself([FromForm] UpdateMyselfDto updateDto, IFormFile? profileImageUrl)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            if (updateDto == null && profileImageUrl == null)
                return ApiResponse.Error("Request body cannot be null.", 400);

            var userId = TokenHelper.GetUserIdFromClaims(User);
            if (userId <= 0)
            {
                _logger.LogWarning("Unauthorized attempt with invalid token.");
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }

            try
            {
                var serviceResponse = await _userService.UpdateMyselfAsync(userId, updateDto, profileImageUrl);

                if (!serviceResponse.Success)
                    return ApiResponse.Error(serviceResponse.ErrorMessage, serviceResponse.StatusCode);

                _logger.LogInformation("Profile updated successfully for user ID {UserId}", userId);
                return ApiResponse.Success(null, "User profile updated successfully.");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found");
                return ApiResponse.Error("User not found", 404);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating user profile");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return ApiResponse.Error("Invalid request data.", 400);

            var userId = TokenHelper.GetUserIdFromClaims(User);

            var serviceResponse = await _userService.ChangePasswordAsync(userId, request);

            if (!serviceResponse.Success)
                return ApiResponse.Error(serviceResponse.ErrorMessage, 400);

            return ApiResponse.Success(null, serviceResponse.Message);
        }
    }
}