using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _uploadDirectory;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileStorageService> _logger;
        private readonly IUserRepository _userRepository;

        public FileStorageService(IConfiguration configuration, IWebHostEnvironment environment, ILogger<FileStorageService> logger, IUserRepository userRepository)
        {
            _uploadDirectory = configuration.GetValue<string>("FileStorage:ProfilePicturePath") ?? "ProfilePicture";
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userRepository = userRepository;
        }

        public async Task<string> SaveProfileImageAsync(IFormFile profileImage, int companyId, int userId)
        {
            _logger.LogInformation("Saving profile image for CompanyId: {CompanyId}, UserId: {UserId}", companyId, userId);

            if (profileImage == null || profileImage.Length == 0)
            {
                _logger.LogWarning("Invalid image file provided for CompanyId: {CompanyId}, UserId: {UserId}", companyId, userId);
                throw new ArgumentException("Invalid image file provided.");
            }

            // Validate image format
            if (!ValidationHelper.IsValidImageFormat(profileImage, new[] { ".jpeg", ".jpg", ".png", ".webp" }))
            {
                _logger.LogWarning("Invalid image format: {FileName}", profileImage.FileName);
                throw new ArgumentException("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.");
            }

            try
            {
                // Generate a unique filename
                var fileName = $"{companyId}_{userId}{Path.GetExtension(profileImage.FileName)}";

                // Get the server directory from the configuration
                string folderPath = _configuration.GetValue<string>("AppSettings:ServerDirectory");
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    _logger.LogError("Server directory is not configured.");
                    throw new InvalidOperationException("Server directory is not configured.");
                }

                // Ensure the directory exists
                if (!Directory.Exists(folderPath))
                {
                    _logger.LogInformation("Creating directory: {FolderPath}", folderPath);
                    Directory.CreateDirectory(folderPath);
                }

                // Construct the full file path
                var filePath = Path.Combine(folderPath, fileName);
                _logger.LogInformation("Saving file to: {FilePath}", filePath);

                // Save the file to the server directory
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                // Get the server URL from configuration
                string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    _logger.LogError("Server URL is not configured.");
                    throw new InvalidOperationException("Server URL is not configured.");
                }

                // Construct the public URL for the profile image
                string profileImageUrl = $"{serverUrl}/ProfilePicture/{fileName}";
                _logger.LogInformation("Profile image saved successfully: {ProfileImageUrl}", profileImageUrl);

                return profileImageUrl; // Return the public URL
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the profile image for CompanyId: {CompanyId}, UserId: {UserId}", companyId, userId);
                throw new CustomFileStorageException("An error occurred while saving the profile image.", ex);
            }
        }

        public async Task<string?> UpdateProfileImageAsync(IFormFile file, int? companyId, int userId)
        {
            var folderPath = _configuration.GetValue<string>("AppSettings:ServerDirectory");
            var serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

            if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(serverUrl))
                throw new InvalidOperationException("Server directory or URL is not configured.");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Delete old image if exists
            var existingImageUrl = await _userRepository.GetProfileImageUrlAsync(userId);
            if (!string.IsNullOrWhiteSpace(existingImageUrl))
                DeleteFileFromStorage(existingImageUrl, folderPath, serverUrl);

            // Save new file
            var profileImageFileName = $"{companyId}_{userId}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folderPath, profileImageFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var profileImageUrl = $"{serverUrl}/ProfilePicture/{profileImageFileName}";
            var dbUpdated = await _userRepository.UpdateProfileImageUrlAsync(userId, profileImageUrl);

            if (!dbUpdated)
            {
                File.Delete(filePath);
                throw new InvalidOperationException("Failed to update database with new image URL.");
            }

            return profileImageUrl;
        }

        public async Task DeleteProfileImageAsync(int userId)
        {
            var folderPath = _configuration.GetValue<string>("AppSettings:ServerDirectory");
            var serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

            var existingImageUrl = await _userRepository.GetProfileImageUrlAsync(userId);
            if (!string.IsNullOrWhiteSpace(existingImageUrl))
            {
                DeleteFileFromStorage(existingImageUrl, folderPath, serverUrl);
                await _userRepository.UpdateProfileImageUrlAsync(userId, null);
            }
        }

        private void DeleteFileFromStorage(string imageUrl, string folderPath, string serverUrl)
        {
            var fileName = imageUrl.Replace($"{serverUrl}/ProfilePicture/", string.Empty);

            fileName = Path.GetFileName(imageUrl);

            var fullPath = Path.Combine(folderPath, fileName);

            if (File.Exists(fullPath))
            {
                _logger.LogInformation("Deleting file: {FullPath}", fullPath);
                File.Delete(fullPath);
            }
        }
    }
}