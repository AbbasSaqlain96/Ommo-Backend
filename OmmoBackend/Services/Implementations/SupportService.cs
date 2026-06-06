using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class SupportService : ISupportService
    {
        private readonly ISupportRequestRepository _supportRequestRepository;
        private readonly ILogger<SupportService> _logger;

        public SupportService(
            ISupportRequestRepository supportRequestRepository,
            ILogger<SupportService> logger)
        {
            _supportRequestRepository = supportRequestRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<object>> CreateSupportRequestAsync(SupportRequestDto request)
        {
            try
            {
                // Required validations
                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    return ServiceResponse<object>.ErrorResponse("Subject is required.", 400);
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return ServiceResponse<object>.ErrorResponse("Message is required.", 400);
                }

                if (string.IsNullOrWhiteSpace(request.ContactEmail))
                {
                    return ServiceResponse<object>.ErrorResponse("Contact email is required.", 400);
                }

                if (!string.IsNullOrWhiteSpace(request.ContactEmail) && !ValidationHelper.IsValidEmail(request.ContactEmail))
                    return ServiceResponse<object>.ErrorResponse("Invalid email address format.", 400);

                _logger.LogInformation("Creating support request for Email: {Email}", request.ContactEmail);

                var supportRequest = new SupportRequest
                {
                    subject = request.Subject.Trim(),
                    message = request.Message.Trim(),
                    contact_email = request.ContactEmail.Trim(),
                    status = "pending",
                    is_ommo_customer = request.IsOmmoExistingCustomer,
                    created_at = DateTimeOffset.UtcNow
                };

                await _supportRequestRepository.AddAsync(supportRequest);

                _logger.LogInformation("Support request created successfully for Email: {Email}", request.ContactEmail);

                return ServiceResponse<object>.SuccessResponse(null, "Your request for support has been submitted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating support request.");

                return ServiceResponse<object>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
