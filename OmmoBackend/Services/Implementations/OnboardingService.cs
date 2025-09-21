using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.ComponentModel.Design;
using System.Numerics;

namespace OmmoBackend.Services.Implementations
{
    public class OnboardingService : IOnboardingService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly ICompanyService _companyService;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly AppDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICarrierRepository _carrierRepository;
        private readonly IConfiguration _configuration;

        public OnboardingService(
            ICompanyRepository companyRepository,
            ICompanyService companyService,
            IUserRepository userRepository,
            IPasswordService passwordService,
            AppDbContext dbContext,
            IUnitOfWork unitOfWork,
            ICarrierRepository carrierRepository,
            IConfiguration configuration)
        {
            _companyRepository = companyRepository;
            _companyService = companyService;
            _userRepository = userRepository;
            _passwordService = passwordService;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _carrierRepository = carrierRepository;
            _configuration = configuration;
        }

        private async Task<string> UploadCompanyLogo(IFormFile companyLogo, int companyId)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerLogoDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server logo directory is not configured.");

            string folderPath = Path.Combine(baseFolderPath);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

            // Save new file
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(companyLogo.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await companyLogo.CopyToAsync(stream);
            }

            return $"{serverUrl}/Logo/{companyId}/{fileName}";
        }
        private async Task<string> UploadUserProfilePicture(IFormFile userProfilePicture, int companyId, int userId)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server profile picture directory is not configured.");

            string folderPath = Path.Combine(baseFolderPath);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

            // Save new file
            //var fileName = $"{Guid.NewGuid()}{Path.GetExtension(userProfilePicture.FileName)}";
            var fileName = $"{companyId}_{userId}{Path.GetExtension(userProfilePicture.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await userProfilePicture.CopyToAsync(stream);
            }

            return $"{serverUrl}/ProfilePicture/{fileName}";
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

        public async Task<DuplicateCheckResult> CheckDuplicateEmailAndPhoneInUserAsync(string email, string phone)
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

        public async Task<ServiceResponse<SignupCompanyResponse>> SignupCompanyAsync(SignupCompanyRequest request)
        {
            // Check for company duplicates
            var duplicateCheckResultForCompanyEntity = await _companyService.CheckDuplicateEmailAndPhoneAsync(request.Email, request.Phone);
            if (duplicateCheckResultForCompanyEntity.HasDuplicate)
                return ServiceResponse<SignupCompanyResponse>.ErrorResponse(duplicateCheckResultForCompanyEntity.Message!, 400);

            // Check for user duplicates
            var duplicateCheckResultForUserEntity = await CheckDuplicateEmailAndPhoneInUserAsync(request.Email, request.Phone);
            if (duplicateCheckResultForUserEntity.HasDuplicate)
                return ServiceResponse<SignupCompanyResponse>.ErrorResponse(duplicateCheckResultForUserEntity.Message!, 400);

            // Validate company type
            if (request.CompanyType != 1 && request.CompanyType != 2)
                return ServiceResponse<SignupCompanyResponse>.ErrorResponse("Invalid company type. Must be 1 (Carrier) or 2 (Dispatcher).", 422);

            // Check MC number for carrier
            if (request.CompanyType == 1)
            {
                if (string.IsNullOrWhiteSpace(request.MCNumber))
                    return ServiceResponse<SignupCompanyResponse>.ErrorResponse("MC number is required for carrier companies.", 400);

                bool isMCNumberDuplicate = await _companyRepository.CheckDuplicateMCNumberAsync(request.MCNumber, request.CompanyType);
                if (isMCNumberDuplicate)
                    return ServiceResponse<SignupCompanyResponse>.ErrorResponse("MC number already exists for another carrier", 409);
            }

            // Logo validation
            if (!ValidationHelper.IsValidImageFormat(request.CompanyLogo, new[] { ".jpeg", ".jpg", ".png", ".webp" }))
                return ServiceResponse<SignupCompanyResponse>.ErrorResponse("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.", 400);

            // Profile URL validation
            if (!ValidationHelper.IsValidImageFormat(request.UserProfilePicture, new[] { ".jpeg", ".jpg", ".png", ".webp" }))
                return ServiceResponse<SignupCompanyResponse>.ErrorResponse("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.", 400);

            // Password Strength check
            var (ok, msg) = ValidatePassword(request.Password);
            if (!ok)
                return ServiceResponse<SignupCompanyResponse>.ErrorResponse(msg, 400);

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // STEP 1 — Create Company
                    var company = new Company
                    {
                        name = request.CompanyName,
                        email = request.Email,
                        phone = request.Phone,
                        address = request.Address,
                        company_type = request.CompanyType,
                        dot_number = request.DOTNumber,
                        logo = "logo path uploading..",
                        fleet_size = request.FleetSize,
                        eld = request.ELD,
                        parent_id = 0,
                        category_type = 1,
                        status = CompanyStatus.active,
                        twilio_number = null,
                        created_at = DateTime.UtcNow
                    };

                    await _companyRepository.AddAsync(company);

                    // upload company logo on server directory
                    string logoUrl = await UploadCompanyLogo(request.CompanyLogo, company.company_id);

                    var recentCompany = await _companyRepository.GetByIdAsync(company.company_id);

                    if (recentCompany == null)
                        throw new Exception("Company not found");

                    // update company logo in database
                    recentCompany.logo = logoUrl;
                    await _companyRepository.UpdateAsync(company);

                    // create a carrier
                    if (request.CompanyType == 1 && !string.IsNullOrWhiteSpace(request.MCNumber))
                    {
                        Carrier carrier = new Carrier();
                        carrier.company_id = company.company_id;
                        carrier.mc_number = request.MCNumber;

                        await _carrierRepository.AddAsync(carrier);
                    }

                    // Hash the user's password
                    _passwordService.HashPassword(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

                    // STEP 2 — Create User
                    var user = new User
                    {
                        user_name = request.Username,
                        user_email = request.Email,
                        phone = request.Phone,
                        password_hash = passwordHash,
                        password_salt = passwordSalt,
                        company_id = company.company_id,
                        role_id = request.RoleID,
                        profile_image_url = "user profile uploading..",
                        status = UserStatus.active
                    };

                    await _userRepository.AddAsync(user);

                    // upload user profile picture on server directory
                    string profilePictureUrl = await UploadUserProfilePicture(request.UserProfilePicture, company.company_id, user.user_id);

                    var recentUser = await _userRepository.GetByIdAsync(user.user_id);

                    if (recentUser == null)
                        throw new Exception("User not found");

                    // update user profile picture in database
                    recentUser.profile_image_url = profilePictureUrl;
                    await _userRepository.UpdateAsync(user);

                    await transaction.CommitAsync();

                    return new ServiceResponse<SignupCompanyResponse>
                    {
                        Data = new SignupCompanyResponse
                        {
                            CompanyId = company.company_id,
                            UserId = user.user_id,
                            Message = "Company and first user created successfully."
                        },
                        Success = true,
                        Message = "Company and first user created successfully."
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return ServiceResponse<SignupCompanyResponse>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }
            });
        }
    }
}
