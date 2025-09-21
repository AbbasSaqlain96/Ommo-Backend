using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/subscription")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ICarrierService _carrierService;
        private readonly IValidator<CreateSubscriptionRequest> _createSubscriptionRequestValidator;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            ICarrierService carrierService,
            IValidator<CreateSubscriptionRequest> createSubscriptionRequestValidator,
            ILogger<SubscriptionController> logger
        )
        {
            _subscriptionService = subscriptionService;
            _carrierService = carrierService;
            _createSubscriptionRequestValidator = createSubscriptionRequestValidator;
            _logger = logger;
        }

        [HttpPost]
        [Route("subscription-request")]
        [Authorize]
        public async Task<IActionResult> PostSubscriptionRequest([FromBody] CreateSubscriptionRequest createSubscriptionRequest)
        {
            _logger.LogInformation("Received request to create a subscription request.");

            var validationResult = await _createSubscriptionRequestValidator.ValidateAsync(createSubscriptionRequest);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Subscription request validation failed: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            try
            {
                // Call the service layer to create the subscription request asynchronously
                await _subscriptionService.CreateSubscriptionRequestAsync(createSubscriptionRequest);
                _logger.LogInformation("Subscription request submitted successfully.");
                return Ok(new { message = "Subscription request submitted successfully." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid subscription request data.");
                return BadRequest("Invalid subscription request data.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a subscription request.");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        [HttpPost]
        [Route("respond-subscription-request")]
        [Authorize]
        public async Task<IActionResult> RespondSubscriptionRequest([FromBody] SubscriptionResponse request)
        {
            _logger.LogInformation("Received request to respond to a subscription request.");

            // Validate the model state; return a Bad Request if the model contains validation errors
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed.");
                return BadRequest(ModelState);
            }

            // Extract CompanyId from token
            int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid Company ID in token.");
                return ApiResponse.Error("Invalid Company ID.", 400);
            }

            try
            {
                // Fetch Carrier Id using the Company Id
                var carrierId = await _carrierService.GetCarrierIdByCompanyIdAsync(companyId);
                if (carrierId == null)
                {
                    _logger.LogWarning("Carrier not found for Company Id: {CompanyId}", companyId);
                    return NotFound("Carrier not found for the provided Company Id.");
                }

                // Call the subscription service to respond to the subscription request
                await _subscriptionService.RespondSubscriptionRequestAsync(carrierId.Value, request);
                _logger.LogInformation("Subscription request status updated successfully for Carrier ID: {CarrierId}", carrierId);
                return Ok(new { message = "Subscription request status updated successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt.");
                // Return an Unauthorized response with the exception message if access is denied
                return Unauthorized("You do not have permission to perform this action.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid subscription response data.");
                // Return a Bad Request response with the exception message for argument validation issues
                return BadRequest("The provided subscription response data is invalid.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while responding to a subscription request.");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        [HttpGet("get-all-subscription-requests/{dispatchServiceId}")]
        [Authorize]
        public async Task<IActionResult> GetAllSubscriptionRequests(int dispatchServiceId)
        {
            _logger.LogInformation("Fetching all subscription requests for Dispatch_Service_ID: {DispatchServiceId}", dispatchServiceId);

            try
            {
                // Retrieve all subscription requests for the given dispatch service Id
                var requests = await _subscriptionService.GetAllRequestsByDispatchServiceIdAsync(dispatchServiceId);

                // Check if no requests found
                if (!requests.Success)
                {
                    _logger.LogWarning("No requests found for Dispatch_Service_ID: {DispatchServiceId}", dispatchServiceId);
                    return NotFound($"No requests found for Dispatch_Service_ID: {dispatchServiceId}");
                }

                _logger.LogInformation("Successfully fetched subscription requests for Dispatch_Service_ID: {DispatchServiceId}", dispatchServiceId);
                // Return the found requests
                return Ok(requests);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid Dispatch Service ID: {DispatchServiceId}", dispatchServiceId);
                // Handle invalid Dispatch Service Id
                return BadRequest("Invalid Dispatch Service ID.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching subscription requests.");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }
    }
}