using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

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
        private readonly ICompanyOnboardingService _companyOnboardingService;
        private readonly IUltravoxService _ultravoxService;
        private readonly IAIAgentService _aIAgentService;
        private readonly IAIAgentSettingService _aIAgentSettingService;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IQuestionnaireRepository _questionnaireRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<OnboardingService> _logger;
        public OnboardingService(
            ICompanyRepository companyRepository,
            ICompanyService companyService,
            IUserRepository userRepository,
            IPasswordService passwordService,
            AppDbContext dbContext,
            IUnitOfWork unitOfWork,
            ICarrierRepository carrierRepository,
            IConfiguration configuration,
            ICompanyOnboardingService companyOnboardingService,
            IUltravoxService ultravoxService,
            IAIAgentService aIAgentService,
            IAIAgentSettingService aIAgentSettingService,
            IOnboardingRepository onboardingRepository,
            IQuestionnaireRepository questionnaireRepository,
            IEmailService emailService,
            ILogger<OnboardingService> logger)
        {
            _companyRepository = companyRepository;
            _companyService = companyService;
            _userRepository = userRepository;
            _passwordService = passwordService;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _carrierRepository = carrierRepository;
            _configuration = configuration;
            _companyOnboardingService = companyOnboardingService;
            _ultravoxService = ultravoxService;
            _aIAgentService = aIAgentService;
            _aIAgentSettingService = aIAgentSettingService;
            _onboardingRepository = onboardingRepository;
            _questionnaireRepository = questionnaireRepository;
            _emailService = emailService;
            _logger = logger;
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

            return $"{serverUrl}/Logo/{fileName}";
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
            _logger.LogInformation("Starting company signup for Email: {Email}", request.Email);

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
                        is_verified = false,
                        verification_status = VerificationStatus.pending,
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

                    // STEP 3 — Company Onboarding
                    await _companyOnboardingService.AddCompanyOnboardingAsync(company.company_id);

                    // STEP 4 — Create Ultravox Agent
                    var agentGuid = await _ultravoxService.CreateAgentAsync(company.company_id);

                    // STEP 5 — Insert into agent table
                    await _aIAgentService.AddAgentAsync(agentGuid, company.company_id);

                    // STEP 6 — Insert agent settings
                    await _aIAgentSettingService.AddAgentSettingAsync(agentGuid);

                    await transaction.CommitAsync();

                    try
                    {
                        await _emailService.SendWelcomeVerificationEmailAsync(
                            company.email,
                            company.name
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to send welcome email for CompanyId: {CompanyId}",
                            company.company_id);
                    }

                    _logger.LogInformation("Company signup completed successfully for CompanyId: {CompanyId}", company.company_id);

                    return new ServiceResponse<SignupCompanyResponse>
                    {
                        Data = new SignupCompanyResponse
                        {
                            OnboardingDto = new OnboardingDto
                            {
                                IsCompleted = false,
                                CurrentStep = OnboardingStep.verification
                            }
                        },
                        Success = true,
                        Message = "Company created successfully."
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex,
                        "Transaction failed during SignupCompany. Email: {Email}, CompanyName: {CompanyName}",
                        request.Email,
                        request.CompanyName);

                    throw;
                }
            });
        }

        public async Task<OnboardingAuthDto> GetOnboardingDataAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching onboarding data for CompanyId: {CompanyId}", companyId);

                var data = await _onboardingRepository.GetOnboardingDataAsync(companyId);

                if (data == null)
                {
                    _logger.LogInformation("No onboarding/payment data found for CompanyId: {CompanyId}", companyId);

                    return new OnboardingAuthDto
                    {
                        IsCompleted = null,
                        CurrentStep = null,
                        SubscriptionStatus = null
                    };
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to fetch onboarding data for CompanyId: {CompanyId}",
                    companyId);

                throw;
            }
        }

        public async Task<ServiceResponse<string>> CompleteQuestionnaireAsync(
            int companyId,
            List<QuestionnaireAnswerRequest> request)
        {
            _logger.LogInformation("Starting questionnaire completion for CompanyId {CompanyId}", companyId);


            // =========================
            // Validation
            // =========================

            if (request == null || request.Count != 3)
            {
                _logger.LogWarning("Invalid questionnaire count for CompanyId {CompanyId}", companyId);

                return ServiceResponse<string>.ErrorResponse(
                    "Invalid request. Exactly 3 answers required with question_number 1, 2, and 3.", 400);
            }

            var validSet = new HashSet<int> { 1, 2, 3 };
            var requestSet = request.Select(x => x.QuestionNumber).ToHashSet();

            if (!validSet.SetEquals(requestSet))
            {
                _logger.LogWarning("Invalid question numbers for CompanyId {CompanyId}", companyId);

                return ServiceResponse<string>.ErrorResponse(
                    "Invalid question numbers. Must include exactly 1, 2, and 3.", 400);
            }

            if (request.Any(x => string.IsNullOrWhiteSpace(x.AnswerText)))
            {
                _logger.LogWarning("Empty answer detected for CompanyId {CompanyId}", companyId);

                return ServiceResponse<string>.ErrorResponse(
                    "Answer text cannot be empty.", 400);
            }

            // =========================
            // Transaction
            // =========================

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // =========================
                    // UPSERT
                    // =========================

                    _logger.LogInformation("Upserting questionnaire answers for CompanyId {CompanyId}", companyId);

                    await _questionnaireRepository.UpsertAnswersAsync(companyId, request);

                    // =========================
                    // Update onboarding
                    // =========================

                    _logger.LogInformation("Updating onboarding step for CompanyId {CompanyId}", companyId);

                    await _onboardingRepository.UpdateToIntegrationStepAsync(companyId);

                    await transaction.CommitAsync();

                    _logger.LogInformation("Questionnaire completed successfully for CompanyId {CompanyId}", companyId);

                    return ServiceResponse<string>.SuccessResponse(null,
                        "Questionnaire submitted successfully.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex,
                        "Transaction failed during questionnaire completion for CompanyId {CompanyId}",
                        companyId);

                    throw;
                }
            });
        }

        public async Task<ServiceResponse<string>> AdvanceToPaymentStepAsync(int companyId)
        {
            _logger.LogInformation("Advancing onboarding to payment step for CompanyId {CompanyId}", companyId);

            try
            {
                await _onboardingRepository.UpdateToPaymentStepAsync(companyId);

                _logger.LogInformation("Onboarding advanced to payment step successfully for CompanyId {CompanyId}", companyId);
                
                return ServiceResponse<string>.SuccessResponse(null,
                    "Onboarding advanced to payment step successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to advance onboarding to payment step for CompanyId {CompanyId}",
                    companyId);
                
                throw;
            }
        }
    }
}
