using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class PlansService : IPlansService
    {
        private readonly ILogger<PlansService> _logger;
        private readonly ICustomPackageRepository _customPackageRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IEmailService _emailService;
        private readonly IPlanRepository _planRepository;
        public PlansService(ILogger<PlansService> logger, ICustomPackageRepository customPackageRepository, ICompanyRepository companyRepository, IEmailService emailService, IPlanRepository planRepository)
        {
            _logger = logger;
            _customPackageRepository = customPackageRepository;
            _companyRepository = companyRepository;
            _emailService = emailService;
            _planRepository = planRepository;
        }

        public async Task<ServiceResponse<string>> RequestCustomPackageAsync(
            int companyId,
            CustomPackageRequestDto request)
        {
            _logger.LogInformation("Custom package request started for CompanyId {CompanyId}", companyId);

            // Validation
            if (string.IsNullOrWhiteSpace(request.Email) ||
                request.EstMinutes <= 0 ||
                request.Concurrency <= 0 ||
                request.AllowedUsers <= 0)
            {
                return ServiceResponse<string>.ErrorResponse(
                    "Email, est_minutes, concurrency, and allowed_users are required and must be valid.",
                    400);
            }

            // Check existing pending
            var exists = await _customPackageRepository.HasPendingRequestAsync(companyId);
            if (exists)
            {
                return ServiceResponse<string>.ErrorResponse(
                    "A custom package request is already pending for your account.",
                    409);
            }

            // Insert
            var entity = new CustomPackageRequest
            {
                company_id = companyId,
                email = request.Email,
                est_minutes = request.EstMinutes,
                concurrency = request.Concurrency,
                message = request.Message,
                allowed_users = request.AllowedUsers,
                created_at = DateTime.UtcNow
            };

            await _customPackageRepository.InsertAsync(entity);

            // Email
            try
            {
                var company = await _companyRepository.GetByIdAsync(companyId);

                var subject = $"New Custom Package Request — {company?.name ?? "Unknown"}";

                var body = $@"
                    Company ID:     {companyId}<br/>
                    Email:          {request.Email}<br/>
                    Est. Min/month: {request.EstMinutes}<br/>
                    Concurrency:    {request.Concurrency}<br/>
                    Allowed Users:  {request.AllowedUsers}<br/>
                    Message:        {request.Message ?? "-"}<br/>
                    Submitted at:   {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                ";

                await _emailService.SendAsync("info@ommo.ai", subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send custom package email for CompanyId {CompanyId}",
                    companyId);
            }

            return ServiceResponse<string>.SuccessResponse(null,
                "Custom package request submitted successfully.");
        }

        public async Task<ServiceResponse<List<PlanDto>>> GetPlansAsync(int companyId)
        {
            try
            {
                var plans = await _planRepository.GetPlansAsync(companyId);

                // never null
                return ServiceResponse<List<PlanDto>>.SuccessResponse(
                    plans ?? new List<PlanDto>(),
                    "Plans fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch plans for CompanyId {CompanyId}", companyId);

                return ServiceResponse<List<PlanDto>>.ErrorResponse(
                    "Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
