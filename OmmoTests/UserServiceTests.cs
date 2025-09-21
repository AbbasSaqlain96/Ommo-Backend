using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Implementations;
using OmmoBackend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmmoTests
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IRoleRepository> _roleRepoMock = new();
        private readonly Mock<ICompanyRepository> _companyRepoMock = new();
        private readonly Mock<IPasswordService> _passwordServiceMock = new();
        private readonly Mock<IWebHostEnvironment> _envMock = new();
        private readonly Mock<ILogger<UserService>> _loggerMock = new();
        private readonly UserService _userService;
        public UserServiceTests()
        {
            _userService = new UserService(
           _userRepoMock.Object,
           _roleRepoMock.Object,
           _companyRepoMock.Object,
           _passwordServiceMock.Object,
           _envMock.Object,
           _loggerMock.Object);
        }

        [Fact]
        public async Task CreateUserSignupAsync_CompanyNotFound_ReturnsErrorResponse()
        {
            var request = GetValidSignupRequest();
            _companyRepoMock.Setup(x => x.ExistsAsync(request.CompanyId)).ReturnsAsync(false);

            var result = await _userService.CreateUserSignupAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Company not found. Please provide a valid Company ID", result.ErrorMessage);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateUserSignupAsync_UserAlreadyExists_ReturnsErrorResponse()
        {
            var request = GetValidSignupRequest();
            _companyRepoMock.Setup(x => x.ExistsAsync(request.CompanyId)).ReturnsAsync(true);
            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync(request.Email, request.Phone)).ReturnsAsync(new User());

            var result = await _userService.CreateUserSignupAsync(request);

            Assert.False(result.Success);
            Assert.Equal("A user with the same email or phone already exists.", result.ErrorMessage);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateUserSignupAsync_InvalidRole_ReturnsErrorResponse()
        {
            var request = GetValidSignupRequest();
            _companyRepoMock.Setup(x => x.ExistsAsync(request.CompanyId)).ReturnsAsync(true);
            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync(request.Email, request.Phone)).ReturnsAsync((User)null);
            _roleRepoMock.Setup(x => x.RoleExistsAsync(request.RoleId)).ReturnsAsync(false);

            var result = await _userService.CreateUserSignupAsync(request);

            Assert.False(result.Success);
            Assert.Contains("Invalid Role", result.ErrorMessage);
        }

        [Theory]
        [InlineData("pending")]
        [InlineData(null)]
        public async Task CreateUserSignupAsync_InvalidStatus_ReturnsErrorResponse(string status)
        {
            var request = GetValidSignupRequest();
            request.Status = status;

            _companyRepoMock.Setup(x => x.ExistsAsync(request.CompanyId)).ReturnsAsync(true);
            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync(request.Email, request.Phone)).ReturnsAsync((User)null);
            _roleRepoMock.Setup(x => x.RoleExistsAsync(request.RoleId)).ReturnsAsync(true);

            var result = await _userService.CreateUserSignupAsync(request);

            Assert.False(result.Success);
            Assert.Contains("Invalid status", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateUserSignupAsync_Success_ReturnsUserId()
        {
            var request = GetValidSignupRequest();
            var passwordHash = new byte[] { 1, 2, 3 };
            var passwordSalt = new byte[] { 4, 5, 6 };

            _companyRepoMock.Setup(x => x.ExistsAsync(request.CompanyId)).ReturnsAsync(true);
            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync(request.Email, request.Phone)).ReturnsAsync((User)null);
            _roleRepoMock.Setup(x => x.RoleExistsAsync(request.RoleId)).ReturnsAsync(true);
            _passwordServiceMock.Setup(x => x.HashPassword(request.Password, out passwordHash, out passwordSalt));
            _userRepoMock.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _userService.CreateUserSignupAsync(request);

            Assert.True(result.Success);
            Assert.Equal(request.Email, result.Data.EmailOrPhone);
            Assert.Equal(Convert.ToBase64String(passwordHash), result.Data.PasswordHash);
            Assert.Equal(Convert.ToBase64String(passwordSalt), result.Data.PasswordSalt);
        }

        [Fact]
        public async Task CreateUserSignupAsync_ExceptionThrown_ReturnsServerError()
        {
            var request = GetValidSignupRequest();
            _companyRepoMock.Setup(x => x.ExistsAsync(request.CompanyId)).ThrowsAsync(new Exception("Unexpected error"));

            var result = await _userService.CreateUserSignupAsync(request);

            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
            Assert.Contains("Server is temporarily unavailable", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsError_WhenUserAlreadyExists()
        {
            // Arrange
            var request = new CreateUserRequest { EmailOrPhone = "test@example.com" };
            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync("test@example.com")).ReturnsAsync(new User());

            // Act
            var result = await _userService.CreateUserAsync(request, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("A user with the same email or phone associated with another user", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsError_WhenRoleIsInvalidOrUnauthorized()
        {
            // Arrange
            var request = new CreateUserRequest { EmailOrPhone = "test@example.com", RoleId = 99 };
            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync((User)null);
            _roleRepoMock.Setup(x => x.GetRoleByIdAsync(99)).ReturnsAsync(new Role { role_cat = RoleCategory.custom, company_id = 2 });

            // Act
            var result = await _userService.CreateUserAsync(request, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Invalid role", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsError_WhenEmailOrPhoneIsInvalid()
        {
            // Arrange
            var request = new CreateUserRequest { EmailOrPhone = "invalid-format", RoleId = 1 };
            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync((User)null);
            _roleRepoMock.Setup(x => x.GetRoleByIdAsync(1)).ReturnsAsync(new Role { role_cat = RoleCategory.custom, company_id = 1 });

            // Act
            var result = await _userService.CreateUserAsync(request, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Invalid Email or Phone format.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsSuccess_WhenDataIsValid_Email()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                EmailOrPhone = "valid@example.com",
                Password = "P@ssw0rd!",
                RoleId = 1,
                Username = "john"
            };
            var passwordHash = new byte[] { 1, 2, 3 };
            var passwordSalt = new byte[] { 4, 5, 6 };

            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync((User)null);
            _roleRepoMock.Setup(x => x.GetRoleByIdAsync(1)).ReturnsAsync(new Role { role_cat = RoleCategory.standard, company_id = 999 });
            _passwordServiceMock.Setup(x => x.HashPassword("P@ssw0rd!", out passwordHash, out passwordSalt));

            _userRepoMock.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _userService.CreateUserAsync(request, 999);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("User created successfully.", result.Message);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsSuccess_WhenDataIsValid_Phone()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                EmailOrPhone = "923001112233",
                Password = "P@ssw0rd!",
                RoleId = 1,
                Username = "john"
            };
            var passwordHash = new byte[] { 1, 2, 3 };
            var passwordSalt = new byte[] { 4, 5, 6 };

            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync((User)null);
            _roleRepoMock.Setup(x => x.GetRoleByIdAsync(1)).ReturnsAsync(new Role { role_cat = RoleCategory.custom, company_id = 1 });
            _passwordServiceMock.Setup(x => x.HashPassword("P@ssw0rd!", out passwordHash, out passwordSalt));
            _userRepoMock.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _userService.CreateUserAsync(request, 1);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("User created successfully.", result.Message);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task CreateUserAsync_Returns503_WhenExceptionThrown()
        {
            // Arrange
            var request = new CreateUserRequest { EmailOrPhone = "error@example.com", RoleId = 1 };
            _userRepoMock.Setup(x => x.FindByEmailOrPhoneAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Boom!"));

            // Act
            var result = await _userService.CreateUserAsync(request, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
            Assert.Equal("Server is temporarily unavailable. Please try again later.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateProfileImageAsync_ShouldUpdateProfileImage_WhenUserExists()
        {
            // Arrange
            var userId = 1;
            var newImagePath = "/images/profile1.jpg";
            var user = new User { user_id = userId, profile_image_url = null };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _userService.UpdateProfileImageAsync(userId, newImagePath);

            // Assert
            Assert.Equal(newImagePath, result);
            _userRepoMock.Verify(repo => repo.UpdateAsync(It.Is<User>(u => u.profile_image_url == newImagePath)), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileImageAsync_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var userId = 99;
            var newImagePath = "/images/profile99.jpg";

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _userService.UpdateProfileImageAsync(userId, newImagePath));
            Assert.Equal("User not found", exception.Message);
            _userRepoMock.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnSuccess_WhenUpdateIsValid()
        {
            // Arrange
            var userId = 1;
            var user = new User { user_id = userId, company_id = 10 };
            var role = new Role { role_id = 2, company_id = 10, role_cat = RoleCategory.standard };

            var request = new UpdateUserRequest
            {
                UserId = userId,
                Username = "UpdatedUser",
                EmailOrPhone = "new@email.com",
                RoleId = 2
            };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);
            _roleRepoMock.Setup(repo => repo.GetByIdAsync(2)).ReturnsAsync(role);
            _userRepoMock.Setup(repo => repo.CheckIfEmailOrPhoneExists("new@email.com", userId)).ReturnsAsync(false);
            _userRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _userService.UpdateUserAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("User updated successfully", result.Message);
            _userRepoMock.Verify(repo => repo.UpdateAsync(It.Is<User>(u =>
                u.user_name == "UpdatedUser" && u.user_email == "new@email.com" && u.role_id == 2
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnError_WhenUserNotFound()
        {
            // Arrange
            var request = new UpdateUserRequest { UserId = 1 };
            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((User)null);

            // Act
            var result = await _userService.UpdateUserAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid User. The provided User ID does not exist", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnError_WhenRoleNotFound()
        {
            // Arrange
            var request = new UpdateUserRequest { UserId = 1, RoleId = 5 };
            var user = new User { user_id = 1 };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _roleRepoMock.Setup(repo => repo.GetByIdAsync(5)).ReturnsAsync((Role)null);

            // Act
            var result = await _userService.UpdateUserAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Role. The specified Role ID either does not exist", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnError_WhenRoleIsFromAnotherCompany()
        {
            // Arrange
            var request = new UpdateUserRequest { UserId = 1, RoleId = 5 };
            var user = new User { user_id = 1, company_id = 1 };
            var role = new Role { role_id = 5, company_id = 2, role_cat = RoleCategory.custom }; // Different company

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _roleRepoMock.Setup(repo => repo.GetByIdAsync(5)).ReturnsAsync(role);

            // Act
            var result = await _userService.UpdateUserAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Role. The specified Role ID either does not exist", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnError_WhenEmailOrPhoneAlreadyExists()
        {
            // Arrange
            var request = new UpdateUserRequest { UserId = 1, EmailOrPhone = "existing@email.com" };
            var user = new User { user_id = 1 };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepoMock.Setup(repo => repo.CheckIfEmailOrPhoneExists("existing@email.com", 1)).ReturnsAsync(true);

            // Act
            var result = await _userService.UpdateUserAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("A user with the same email or phone", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturnError_WhenEmailOrPhoneFormatIsInvalid()
        {
            // Arrange
            var request = new UpdateUserRequest { UserId = 1, EmailOrPhone = "invalid#input" };
            var user = new User { user_id = 1 };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepoMock.Setup(repo => repo.CheckIfEmailOrPhoneExists("invalid#input", 1)).ReturnsAsync(false);

            // Act
            var result = await _userService.UpdateUserAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Email or Phone format", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldReturn503_OnUnhandledException()
        {
            // Arrange
            var request = new UpdateUserRequest { UserId = 1 };
            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ThrowsAsync(new Exception("Database unreachable"));

            // Act
            var result = await _userService.UpdateUserAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
            Assert.Equal("Server is temporarily unavailable. Please try again later.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsError_WhenUserNotFound()
        {
            // Arrange
            var request = new UpdateUserRequest { UserId = 1 };
            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((User)null);

            // Act
            var result = await _userService.UpdateUserAsync(request, null, CreateFakeHttpRequest());

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsError_WhenRoleNotFound()
        {
            // Arrange
            var user = new User { user_id = 1 };
            var request = new UpdateUserRequest { UserId = 1, RoleId = 99 };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _roleRepoMock.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((Role)null);

            // Act
            var result = await _userService.UpdateUserAsync(request, null, CreateFakeHttpRequest());

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid role.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsError_WhenRoleCompanyMismatch()
        {
            var user = new User { user_id = 1, company_id = 100 };
            var role = new Role { role_id = 2, role_cat = RoleCategory.custom, company_id = 200 };
            var request = new UpdateUserRequest { UserId = 1, RoleId = 2 };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _roleRepoMock.Setup(repo => repo.GetByIdAsync(2)).ReturnsAsync(role);

            var result = await _userService.UpdateUserAsync(request, null, CreateFakeHttpRequest());

            Assert.False(result.Success);
            Assert.Contains("does not belong", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsError_WhenEmailOrPhoneIsInvalid()
        {
            var user = new User { user_id = 1 };
            var request = new UpdateUserRequest { UserId = 1, EmailOrPhone = "not-an-email-or-phone" };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);

            var result = await _userService.UpdateUserAsync(request, null, CreateFakeHttpRequest());

            Assert.False(result.Success);
            Assert.Equal("Invalid Email or Phone format.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserAsync_UpdatesUserSuccessfully_WithEmail()
        {
            var user = new User { user_id = 1, company_id = 1 };
            var request = new UpdateUserRequest
            {
                UserId = 1,
                EmailOrPhone = "test@example.com",
                Username = "UpdatedName",
                RoleId = 3
            };

            var role = new Role { role_id = 3, role_cat = RoleCategory.standard, company_id = 1 };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _roleRepoMock.Setup(repo => repo.GetByIdAsync(3)).ReturnsAsync(role);
            _userRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _userService.UpdateUserAsync(request, null, CreateFakeHttpRequest());

            Assert.True(result.Success);
            Assert.Equal("UpdatedName", user.user_name);
            Assert.Equal("test@example.com", user.user_email);
            Assert.Null(user.phone);
        }

        [Fact]
        public async Task UpdateUserAsync_UpdatesUserSuccessfully_WithPhone()
        {
            var user = new User { user_id = 1, company_id = 1 };
            var request = new UpdateUserRequest
            {
                UserId = 1,
                EmailOrPhone = "1234567890"
            };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _userService.UpdateUserAsync(request, null, CreateFakeHttpRequest());

            Assert.True(result.Success);
            Assert.Equal("1234567890", user.phone);
            Assert.Null(user.user_email);
        }

        [Fact]
        public async Task UpdateUserAsync_DeletesOldImage_WhenProfileImageUrlProvided()
        {    
            // Arrange
            var user = new User
            {
                user_id = 1,
                company_id = 1,
                profile_image_url = "https://localhost/ProfilePicture/old.png"
            };

            var request = new UpdateUserRequest { UserId = 1 };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Create dummy old image file
            var relativePath = "ProfilePicture/old.png";
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            var directory = Path.GetDirectoryName(imagePath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(imagePath, "dummy"); // create dummy file

            // Act
            var result = await _userService.UpdateUserAsync(request, "https://localhost/ProfilePicture/new.png", CreateFakeHttpRequest());

            // Assert
            Assert.True(result.Success);
            Assert.Equal("https://localhost/ProfilePicture/new.png", user.profile_image_url);
            Assert.False(File.Exists(imagePath));
        }

        [Fact]
        public async Task UserBelongsToCompanyAsync_ReturnsTrue_WhenUserBelongsToCompany()
        {
            // Arrange
            var userId = 1;
            var companyId = 100;

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(new User { user_id = userId, company_id = companyId });

            // Act
            var result = await _userService.UserBelongsToCompanyAsync(userId, companyId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UserBelongsToCompanyAsync_ReturnsFalse_WhenUserDoesNotBelongToCompany()
        {
            // Arrange
            var userId = 1;
            var companyId = 100;
            var differentCompanyId = 200;

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(new User { user_id = userId, company_id = differentCompanyId });

            // Act
            var result = await _userService.UserBelongsToCompanyAsync(userId, companyId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UserBelongsToCompanyAsync_ReturnsFalse_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = 1;
            var companyId = 100;

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userService.UserBelongsToCompanyAsync(userId, companyId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsSuccess_WhenUserExists()
        {
            // Arrange
            int userId = 1;
            var mockUser = new User
            {
                user_id = userId,
                user_name = "JohnDoe",
                user_email = "john@example.com",
                phone = "1234567890",
                company_id = 101,
                role_id = 2,
                profile_image_url = "http://image.url"
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(mockUser);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("JohnDoe", result.Data.Username);
            Assert.Equal("john@example.com", result.Data.Email);
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            int userId = 1;

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
            Assert.Null(result.Data);
            Assert.Equal("User not found", result.ErrorMessage);
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsError_WhenExceptionIsThrown()
        {
            // Arrange
            int userId = 1;

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
            Assert.Equal("An error occurred while fetching the user details", result.ErrorMessage);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task CheckIfEmailOrPhoneExistsAsync_ReturnsTrue_WhenDuplicateExists()
        {
            // Arrange
            var email = "test@example.com";
            var phone = "1234567890";

            _userRepoMock
                .Setup(repo => repo.CheckDuplicateEmailOrPhoneAsync(email, phone))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.CheckIfEmailOrPhoneExistsAsync(email, phone);

            // Assert
            Assert.True(result);
            _userRepoMock.Verify(repo => repo.CheckDuplicateEmailOrPhoneAsync(email, phone), Times.Once);
        }

        [Fact]
        public async Task CheckIfEmailOrPhoneExistsAsync_ReturnsFalse_WhenNoDuplicateExists()
        {
            // Arrange
            var email = "new@example.com";
            var phone = "0987654321";

            _userRepoMock
                .Setup(repo => repo.CheckDuplicateEmailOrPhoneAsync(email, phone))
                .ReturnsAsync(false);

            // Act
            var result = await _userService.CheckIfEmailOrPhoneExistsAsync(email, phone);

            // Assert
            Assert.False(result);
            _userRepoMock.Verify(repo => repo.CheckDuplicateEmailOrPhoneAsync(email, phone), Times.Once);
        }

        [Fact]
        public async Task UpdateUserPasswordAsync_ReturnsSuccess_WhenUserExists()
        {
            // Arrange
            var identifier = "test@example.com";
            var newPassword = "newPassword123";
            var user = new User { user_email = identifier };

            _userRepoMock.Setup(repo => repo.FindByEmailOrPhoneAsync(identifier))
                               .ReturnsAsync(user);

            _passwordServiceMock.Setup(ps => ps.HashPassword(newPassword, out It.Ref<byte[]>.IsAny, out It.Ref<byte[]>.IsAny))
                                .Callback(new HashPasswordCallback((string password, out byte[] hash, out byte[] salt) =>
                                {
                                    hash = new byte[] { 1, 2, 3 };
                                    salt = new byte[] { 4, 5, 6 };
                                }));

            _userRepoMock.Setup(repo => repo.UpdateAsync(user)).Returns(Task.CompletedTask);

            // Act
            var result = await _userService.UpdateUserPasswordAsync(identifier, newPassword);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Password updated successfully.", result.Message);
            _userRepoMock.Verify(repo => repo.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateUserPasswordAsync_ReturnsError_WhenUserNotFound()
        {
            // Arrange
            var identifier = "notfound@example.com";
            var newPassword = "newPassword123";

            _userRepoMock.Setup(repo => repo.FindByEmailOrPhoneAsync(identifier))
                               .ReturnsAsync((User)null);

            // Act
            var result = await _userService.UpdateUserPasswordAsync(identifier, newPassword);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("Identifier not found. Please check the provided email or phone number.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateUserPasswordAsync_ReturnsError_WhenExceptionOccurs()
        {
            // Arrange
            var identifier = "test@example.com";
            var newPassword = "newPassword123";
            var user = new User { user_email = identifier };

            _userRepoMock.Setup(repo => repo.FindByEmailOrPhoneAsync(identifier))
                               .ReturnsAsync(user);

            _passwordServiceMock.Setup(ps => ps.HashPassword(newPassword, out It.Ref<byte[]>.IsAny, out It.Ref<byte[]>.IsAny))
                                .Callback(new HashPasswordCallback((string password, out byte[] hash, out byte[] salt) =>
                                {
                                    hash = new byte[] { 1, 2, 3 };
                                    salt = new byte[] { 4, 5, 6 };
                                }));

            _userRepoMock.Setup(repo => repo.UpdateAsync(user))
                               .ThrowsAsync(new Exception("DB update error"));

            // Act
            var result = await _userService.UpdateUserPasswordAsync(identifier, newPassword);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
            Assert.Equal("Server is temporarily unavailable. Please try again later.", result.ErrorMessage);
        }


        [Fact]
        public async Task GetCompanyIdByUserId_ReturnsCompanyId_WhenExists()
        {
            // Arrange
            int userId = 1;
            int expectedCompanyId = 101;
            _userRepoMock.Setup(repo => repo.GetCompanyId(userId)).ReturnsAsync(expectedCompanyId);

            // Act
            var result = await _userService.GetCompanyIdByUserId(userId);

            // Assert
            Assert.Equal(expectedCompanyId, result);
            _userRepoMock.Verify(repo => repo.GetCompanyId(userId), Times.Once);
        }

        [Fact]
        public async Task GetCompanyIdByUserId_ThrowsKeyNotFoundException_WhenCompanyIdIsNull()
        {
            // Arrange
            int userId = 2;
            _userRepoMock.Setup(repo => repo.GetCompanyId(userId)).ReturnsAsync((int?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.GetCompanyIdByUserId(userId));
            Assert.Equal($"No company ID found for user with ID: {userId}", exception.Message);
        }

        [Fact]
        public async Task GetCompanyIdByUserId_ThrowsApplicationException_WhenUnexpectedErrorOccurs()
        {
            // Arrange
            int userId = 3;
            _userRepoMock.Setup(repo => repo.GetCompanyId(userId)).ThrowsAsync(new Exception("DB error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ApplicationException>(() => _userService.GetCompanyIdByUserId(userId));
            Assert.Equal("An error occurred while retrieving the company ID.", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("DB error", exception.InnerException.Message);
        }

        [Fact]
        public async Task ToggleUserStatusAsync_ShouldToggleStatus_WhenUserExistsAndBelongsToCompany()
        {
            // Arrange
            var userId = 1;
            var companyId = 10;
            var user = new User
            {
                user_id = userId,
                company_id = companyId,
                status = UserStatus.active
            };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _userService.ToggleUserStatusAsync(userId, companyId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("User status updated successfully.", result.Message);
            Assert.Equal(UserStatus.in_active, user.status); // Should have been toggled
            _userRepoMock.Verify(repo => repo.UpdateAsync(It.Is<User>(u => u.user_id == userId && u.status == UserStatus.in_active)), Times.Once);
        }

        [Fact]
        public async Task ToggleUserStatusAsync_ShouldReturnError_WhenUserNotFoundOrCompanyMismatch()
        {
            // Arrange
            var userId = 2;
            var companyId = 20;

            // Case: User is null
            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _userService.ToggleUserStatusAsync(userId, companyId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Invalid User. The provided User ID does not exist", result.ErrorMessage);
        }

        [Fact]
        public async Task ToggleUserStatusAsync_ShouldReturnError_WhenUnauthorizedAccessExceptionThrown()
        {
            // Arrange
            var userId = 3;
            var companyId = 30;

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ThrowsAsync(new UnauthorizedAccessException("Unauthorized"));

            // Act
            var result = await _userService.ToggleUserStatusAsync(userId, companyId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(401, result.StatusCode);
            Assert.Equal("You do not have permission to access this resource", result.ErrorMessage);
        }

        [Fact]
        public async Task ToggleUserStatusAsync_ShouldReturnError_WhenUnhandledExceptionThrown()
        {
            // Arrange
            var userId = 4;
            var companyId = 40;

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ThrowsAsync(new Exception("Database failure"));

            // Act
            var result = await _userService.ToggleUserStatusAsync(userId, companyId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
            Assert.Equal("Server is temporarily unavailable. Please try again later.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetUsersByCompanyAsync_ShouldReturnUsers_WhenUsersExist()
        {
            // Arrange
            var companyId = 1;
            var users = new List<UserRoleDto>
        {
            new UserRoleDto { UserId = 1, Name = "Alice", RoleName = "Admin" },
            new UserRoleDto { UserId = 2, Name = "Bob", RoleName = "User" }
        };

            _userRepoMock.Setup(repo => repo.GetUsersByCompanyIdAsync(companyId)).ReturnsAsync(users);

            // Act
            var result = await _userService.GetUsersByCompanyAsync(companyId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.Count());
            Assert.Equal("Alice", result.Data.First().Name);
        }

        [Fact]
        public async Task GetUsersByCompanyAsync_ShouldReturnError_WhenNoUsersFound()
        {
            // Arrange
            var companyId = 2;
            _userRepoMock.Setup(repo => repo.GetUsersByCompanyIdAsync(companyId)).ReturnsAsync(new List<UserRoleDto>());

            // Act
            var result = await _userService.GetUsersByCompanyAsync(companyId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("No users found for the provided company Id.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetUsersByCompanyAsync_ShouldReturnError_WhenExceptionThrown()
        {
            // Arrange
            var companyId = 3;
            _userRepoMock.Setup(repo => repo.GetUsersByCompanyIdAsync(companyId)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _userService.GetUsersByCompanyAsync(companyId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
            Assert.Equal("Server is temporarily unavailable. Please try again later.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateMyselfAsync_ShouldUpdateProfile_WhenValidData()
        {
            // Arrange
            int userId = 1;
            var updateDto = new UpdateMyselfDto { Username = "NewUsername" };
            string profileImageUrl = "https://image.com/photo.jpg";

            var existingUser = new User { user_id = userId, user_name = "OldName", profile_image_url = "old.jpg" };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(existingUser);
            _userRepoMock.Setup(repo => repo.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _userService.UpdateMyselfAsync(userId, updateDto, profileImageUrl);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
            Assert.Equal("User updated successfully.", result.Message);
            Assert.Equal("NewUsername", existingUser.user_name);
            Assert.Equal(profileImageUrl, existingUser.profile_image_url);
        }

        [Fact]
        public async Task UpdateMyselfAsync_ShouldReturnError_WhenUserNotFound()
        {
            // Arrange
            int userId = 2;
            var updateDto = new UpdateMyselfDto { Username = "User" };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _userService.UpdateMyselfAsync(userId, updateDto, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("User not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateMyselfAsync_ShouldReturnError_WhenUsernameTooLong()
        {
            // Arrange
            int userId = 3;
            var longUsername = new string('a', 51);
            var updateDto = new UpdateMyselfDto { Username = longUsername };
            var existingUser = new User { user_id = userId };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(existingUser);

            // Act
            var result = await _userService.UpdateMyselfAsync(userId, updateDto, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Username must not exceed 50 characters.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateMyselfAsync_ShouldReturnServerError_OnUnhandledException()
        {
            // Arrange
            int userId = 4;
            var updateDto = new UpdateMyselfDto { Username = "User" };

            _userRepoMock.Setup(repo => repo.GetByIdAsync(userId)).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _userService.UpdateMyselfAsync(userId, updateDto, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
            Assert.Equal("Server is temporarily unavailable. Please try again later.", result.ErrorMessage);
        }

        private CreateUserSignupRequest GetValidSignupRequest()
        {
            return new CreateUserSignupRequest
            {
                Username = "testuser",
                Email = "test@example.com",
                Phone = "1234567890",
                Password = "Password123",
                CompanyId = 1,
                RoleId = 2,
                Status = "active"
            };
        }

        private HttpRequest CreateFakeHttpRequest()
        {
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Scheme).Returns("https");
            mockRequest.Setup(r => r.Host).Returns(new HostString("localhost"));
            return mockRequest.Object;
        }

        // Delegate needed for mocking out parameters in Moq
        private delegate void HashPasswordCallback(string password, out byte[] hash, out byte[] salt);

    }
}
